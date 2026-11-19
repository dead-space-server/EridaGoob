#!/usr/bin/env python3

"""
Sends updates to a Discord webhook for new changelog entries since the last GitHub Actions publish run.

Automatically figures out the last run and changelog contents with the GitHub API.
"""

import json
import os
import re
import time
from datetime import datetime, timezone
from pathlib import Path
from typing import Any, Iterable
from urllib.parse import parse_qsl, urlencode, urlparse

import requests
import yaml

DEBUG = False
DEBUG_CHANGELOG_FILE_OLD = Path("Resources/Changelog/Old.yml")
GITHUB_API_URL = os.environ.get("GITHUB_API_URL", "https://api.github.com")

CHANGELOG_FILE = "Resources/Changelog/EridaChangelog.yml" # Erida edit

# https://discord.com/developers/docs/components/reference
# All text in a components message counts towards one 4000 character limit.
MESSAGE_TEXT_BUDGET = 3600
MESSAGE_COMPONENT_BUDGET = 35
MAX_CHANGE_MESSAGE_LENGTH = 500
DISCORD_MESSAGE_DELAY_SECONDS = 0.5
DISCORD_COMPONENTS_V2_FLAG = 1 << 15
DISCORD_MEDIA_GALLERY_MAX_ITEMS = 10
ERIDA_EMBED_COLOR = 0x9B59B6

TYPE_ORDER = ["Add", "Tweak", "Fix", "Remove"]
TYPES_TO_EMOJI = {
    "Add": "<:cl_add:1572959084565504081>",
    "Tweak": "<:cl_tweak:1572959155965395055>",
    "Fix": "<:cl_fix:1572959108594667590>",
    "Remove": "<:cl_remove:1572959134490558634>",
}
TYPES_TO_LABELS = {
    "Add": "Добавлено",
    "Tweak": "Изменено",
    "Fix": "Исправлено",
    "Remove": "Удалено",
}
OTHER_LABEL = "Прочее"

HTTP_TIMEOUT_SECONDS = 30
MEDIA_MAX_DOWNLOAD_SIZE = 10 * 1024 * 1024
# Webhook uploads share the server's file limit, 10 MB without boosts.
MEDIA_MAX_TOTAL_SIZE = 10 * 1024 * 1024
MEDIA_READ_CHUNK_SIZE = 64 * 1024
MEDIA_FILE_EXTENSIONS = frozenset({
    ".apng", ".avif", ".bmp", ".gif", ".jpeg", ".jpg",
    ".m4v", ".mkv", ".mov", ".mp4", ".png", ".webm", ".webp",
})

ChangelogEntry = dict[str, Any]
Component = dict[str, Any]


class MediaTypeError(RuntimeError):
    pass


def main():
    if not os.environ.get("DISCORD_WEBHOOK_URL_ERIDA"):
        raise RuntimeError("DISCORD_WEBHOOK_URL_ERIDA is not set")

    if DEBUG:
        # to debug this script locally, you can use
        # a separate local file as the old changelog
        last_changelog_stream = DEBUG_CHANGELOG_FILE_OLD.read_text()
        publish_context = None
    else:
        # when running this normally in a GitHub actions workflow,
        # it will get the old changelog from the GitHub API
        last_changelog_stream, publish_context = get_last_changelog()

    last_changelog = yaml.safe_load(last_changelog_stream)
    with open(CHANGELOG_FILE, "r") as f:
        cur_changelog = yaml.safe_load(f)

    entries = list(group_changelog_entries_by_pull_request(
        diff_changelog(last_changelog, cur_changelog)
    ))

    if not entries:
        print("No new changelog entries since the last successful publish")
        return

    change_count = sum(len(entry["changes"]) for entry in entries)
    print(
        f"Sending {len(entries)} pull request changelog entries "
        f"with {change_count} changes since the last successful publish"
    )
    send_changelog(entries, publish_context)


# region GitHub

def get_last_changelog() -> tuple[str, dict[str, Any]]:
    github_repository = os.environ["GITHUB_REPOSITORY"]
    github_run = os.environ["GITHUB_RUN_ID"]
    github_token = os.environ["GITHUB_TOKEN"]

    session = requests.Session()
    session.headers["Authorization"] = f"Bearer {github_token}"
    session.headers["Accept"] = "application/vnd.github+json"
    session.headers["X-GitHub-Api-Version"] = "2022-11-28"

    current_run = get_current_run(session, github_repository, github_run)
    most_recent = get_most_recent_workflow(session, current_run)
    last_sha = most_recent["head_sha"]
    print(f"Last successful publish job was {most_recent['id']}: {last_sha}")
    last_changelog_stream = get_last_changelog_by_sha(
        session, last_sha, github_repository
    )

    return last_changelog_stream, {
        "github_repository": github_repository,
        "current_run": current_run,
        "previous_run": most_recent,
    }


def get_current_run(
    sess: requests.Session, github_repository: str, github_run: str
) -> Any:
    resp = sess.get(
        f"{GITHUB_API_URL}/repos/{github_repository}/actions/runs/{github_run}"
    )
    resp.raise_for_status()
    return resp.json()


def get_most_recent_workflow(
    sess: requests.Session, current_run: dict[str, Any]
) -> Any:
    past_runs = get_past_runs(sess, current_run)
    for run in past_runs["workflow_runs"]:
        # First past successful run that isn't our current run.
        if run["id"] == current_run["id"]:
            continue

        return run

    raise RuntimeError("No previous successful publish workflow run found")


def get_past_runs(sess: requests.Session, current_run: Any) -> Any:
    """
    Get all successful workflow runs before our current one.
    """
    params = {"status": "success", "created": f"<={current_run['created_at']}"}
    resp = sess.get(f"{current_run['workflow_url']}/runs", params=params)
    resp.raise_for_status()
    return resp.json()


def get_last_changelog_by_sha(
    sess: requests.Session, sha: str, github_repository: str
) -> str:
    """
    Use GitHub API to get the previous version of the changelog YAML (Actions builds are fetched with a shallow clone)
    """
    resp = sess.get(
        f"{GITHUB_API_URL}/repos/{github_repository}/contents/{CHANGELOG_FILE}",
        headers={"Accept": "application/vnd.github.raw"},
        params={"ref": sha},
    )
    resp.raise_for_status()
    return resp.text

# endregion


# region Changelog

def diff_changelog(
    old: dict[str, Any], cur: dict[str, Any]
) -> Iterable[ChangelogEntry]:
    """
    Find all new entries not present in the previous publish.
    """
    old_entry_keys = {get_entry_pull_request_key(e) for e in old.get("Entries", [])}
    return (
        e
        for e in cur.get("Entries", [])
        if get_entry_pull_request_key(e) not in old_entry_keys
    )


def group_changelog_entries_by_pull_request(
    entries: Iterable[ChangelogEntry],
) -> Iterable[ChangelogEntry]:
    """
    Merges entries of the same PR so each PR is referenced once.
    """
    grouped: dict[str, ChangelogEntry] = {}

    for entry in entries:
        changes = list(entry.get("changes") or [])
        if not changes:
            print(f"Skipping changelog entry {entry.get('id')} without changes")
            continue

        key = get_entry_pull_request_key(entry)
        existing = grouped.get(key)
        if existing:
            existing["changes"].extend(changes)
            existing["media"] = list(existing.get("media") or []) + list(entry.get("media") or [])
            continue

        grouped[key] = dict(entry, changes=changes)

    return grouped.values()


def get_entry_pull_request_key(entry: ChangelogEntry) -> str:
    url = str(entry.get("url") or "").strip()
    if url:
        return url

    return f"{entry.get('id')}:{entry.get('title')}"


def get_pull_request_number(url: Any) -> str | None:
    match = re.search(r"/pull/(\d+)", str(url or ""))
    return match.group(1) if match else None


def format_pull_request_ref(entry: ChangelogEntry) -> str:
    url = str(entry.get("url") or "").strip()
    number = get_pull_request_number(url)
    if not url or not number:
        return ""

    return f"[`#{number}`]({url})"

# endregion


# region Message layout

def text(content: str) -> Component:
    return {"type": 10, "content": content}


def separator(divider: bool) -> Component:
    return {"type": 14, "divider": divider, "spacing": 1}


def build_header(publish_context: dict[str, Any] | None, role_id: str | None) -> str:
    context = publish_context or {}
    current_run = context.get("current_run") or {}
    previous_run = context.get("previous_run") or {}
    repository = context.get("github_repository")

    meta = [format_discord_date(current_run.get("updated_at") or current_run.get("created_at"))]

    current_sha = current_run.get("head_sha")
    if repository and current_sha:
        previous_sha = previous_run.get("head_sha")
        target = (
            f"compare/{previous_sha}...{current_sha}" if previous_sha
            else f"tree/{current_sha}"
        )
        meta.append(f"[`{current_sha[:7]}`](https://github.com/{repository}/{target})")

    if role_id:
        meta.append(f"<@&{role_id}>")

    return "# Обновление сервера\n-# " + " · ".join(meta)


def format_discord_date(value: Any) -> str:
    timestamp = datetime.now(timezone.utc)
    if value:
        try:
            timestamp = datetime.fromisoformat(str(value).replace("Z", "+00:00"))
        except ValueError:
            pass

    if timestamp.tzinfo is None:
        timestamp = timestamp.replace(tzinfo=timezone.utc)

    return f"<t:{int(timestamp.timestamp())}:D>"


def get_change_type(change: dict[str, Any]) -> str:
    change_type = str(change.get("type") or "")
    return change_type if change_type in TYPES_TO_LABELS else OTHER_LABEL


def get_change_message(change: dict[str, Any]) -> str:
    return truncate(str(change.get("message") or "").strip(), MAX_CHANGE_MESSAGE_LENGTH)


def strip_title_tag(title: str) -> str:
    return re.sub(r"^\s*\[[^\]]*\]\s*", "", title) or title


def media_gallery(media: list[tuple]) -> Component:
    items = []
    for name, _, _, _, entry in media:
        item: dict[str, Any] = {"media": {"url": f"attachment://{name}"}}
        title = truncate(str(entry.get("title") or ""), 1000)
        if title:
            item["description"] = title
        items.append(item)

    return {"type": 12, "items": items}


def build_highlight_blocks(entries: list[ChangelogEntry], media: list[tuple]) -> list[list[Component]]:
    """
    Highlighted PRs go first with their own title, every change line and their media right below.
    """
    blocks: list[list[Component]] = []
    for entry in entries:
        lines = [f"## {strip_title_tag(str(entry.get('title') or 'Без названия'))}"]
        for change in entry["changes"]:
            message = get_change_message(change)
            if not message:
                continue
            emoji = TYPES_TO_EMOJI.get(get_change_type(change), "▪️")
            lines.append(f"{emoji} {message}")

        lines[-1] = f"{lines[-1]} {format_pull_request_ref(entry)}".rstrip()

        block = [separator(True), text(truncate("\n".join(lines), MESSAGE_TEXT_BUDGET))]
        entry_media = [m for m in media if m[4] is entry]
        if entry_media:
            block.append(media_gallery(entry_media))
        blocks.append(block)

    if blocks:
        blocks[-1].append(separator(True))

    return blocks


def build_section_blocks(entries: list[ChangelogEntry]) -> list[list[Component]]:
    """
    Groups every change by type, splitting a section when it outgrows one message.
    """
    by_type: dict[str, list[str]] = {}
    for entry in entries:
        ref = format_pull_request_ref(entry)
        for change in entry["changes"]:
            message = get_change_message(change)
            if message:
                by_type.setdefault(get_change_type(change), []).append(f"- {message} {ref}".rstrip())

    sections: list[str] = []
    for change_type in [*TYPE_ORDER, OTHER_LABEL]:
        lines = by_type.get(change_type)
        if not lines:
            continue

        emoji = TYPES_TO_EMOJI.get(change_type)
        label = TYPES_TO_LABELS.get(change_type, OTHER_LABEL)
        title = f"### {emoji} {label}" if emoji else f"### {label}"

        current = title
        for line in lines:
            if len(current) + len(line) + 1 > MESSAGE_TEXT_BUDGET:
                sections.append(current)
                current = title
            current += "\n" + line
        sections.append(current)

    return [[separator(False), text(section)] for section in sections]


def build_media_block(media: list[tuple]) -> list[Component]:
    if not media:
        return []

    refs = list(dict.fromkeys(format_pull_request_ref(m[4]) for m in media))
    caption = "### Скриншоты"
    if any(refs):
        caption += "\n-# " + " ".join(r for r in refs if r)

    return [separator(False), text(caption), media_gallery(media)]


def build_footer(entries: list[ChangelogEntry], media_links: list[str]) -> list[Component]:
    authors = list(dict.fromkeys(
        str(entry.get("author") or "").strip() for entry in entries
    ))
    lines = [f"🔗 {url}" for url in media_links]
    lines.append("-# Авторы: " + (", ".join(a for a in authors if a) or "неизвестно"))
    return [separator(True), text(truncate("\n".join(lines), MESSAGE_TEXT_BUDGET))]


def block_length(block: list[Component]) -> int:
    return sum(len(c.get("content", "")) for c in block)


def pack_messages(header: str, blocks: list[list[Component]]) -> list[list[Component]]:
    """
    Packs blocks in order into as few containers as the per-message text and component limits allow.
    """
    messages: list[list[Component]] = []
    current: list[Component] = [text(header)]
    length = len(header)

    for block in blocks:
        # A block never exceeds the budget, so a message holding only its first line always takes it.
        overflows = (
            length + block_length(block) > MESSAGE_TEXT_BUDGET
            or len(current) + len(block) > MESSAGE_COMPONENT_BUDGET
        )
        if overflows and len(current) > 1:
            messages.append(current)
            current = [text("-# Продолжение")]
            length = 0

        # A divider closing the previous block already separates it from the next one.
        if current[-1]["type"] == 14 and block[0]["type"] == 14:
            block = block[1:]

        current += block
        length += block_length(block)

    messages.append(current)
    return [
        [{"type": 17, "accent_color": ERIDA_EMBED_COLOR, "components": components}]
        for components in messages
    ]

# endregion


# region Media

def iter_media_urls(entry: ChangelogEntry) -> Iterable[str]:
    media = entry.get("media") or []
    if not isinstance(media, list):
        return
    seen: set[str] = set()
    for value in media:
        url = str(value or "").strip()
        parsed = urlparse(url)
        if parsed.scheme not in {"http", "https"} or not parsed.netloc:
            continue
        if parsed.username or parsed.password or len(url) > 2000:
            continue
        if url in seen:
            continue
        seen.add(url)
        yield url


def detect_media_type(data: bytes) -> tuple[str, str]:
    if data.startswith(b"\xff\xd8\xff"):
        return "image/jpeg", ".jpg"
    if data.startswith(b"\x89PNG\r\n\x1a\n"):
        return "image/png", ".png"
    if data.startswith((b"GIF87a", b"GIF89a")):
        return "image/gif", ".gif"
    if data.startswith(b"RIFF") and data[8:12] == b"WEBP":
        return "image/webp", ".webp"
    if data.startswith(b"\x1a\x45\xdf\xa3"):
        return "video/webm", ".webm"
    if len(data) >= 12 and data[4:8] == b"ftyp":
        if data[8:12] == b"qt  ":
            return "video/quicktime", ".mov"
        return "video/mp4", ".mp4"
    raise MediaTypeError("unrecognized media content")


def download_media(session: requests.Session, url: str) -> tuple[str, str, bytes] | None:
    try:
        response = session.get(url, stream=True, timeout=HTTP_TIMEOUT_SECONDS)
        response.raise_for_status()

        length = response.headers.get("Content-Length")
        if length and length.isdigit() and int(length) > MEDIA_MAX_DOWNLOAD_SIZE:
            print(f"Media too large ({url}): {length} bytes")
            return None

        data = bytearray()
        for chunk in response.iter_content(chunk_size=MEDIA_READ_CHUNK_SIZE):
            data.extend(chunk)
            if len(data) > MEDIA_MAX_DOWNLOAD_SIZE:
                print(f"Media exceeded the size cap ({url})")
                return None
    except requests.RequestException as exc:
        print(f"Failed to download media ({url}): {exc}")
        return None

    try:
        mime, ext = detect_media_type(bytes(data))
    except MediaTypeError:
        ext = Path(urlparse(url).path).suffix.lower()
        if ext not in MEDIA_FILE_EXTENSIONS:
            print(f"Unrecognized media type ({url}); falling back to a link")
            return None
        mime = "application/octet-stream"

    return ext, mime, bytes(data)


def collect_media(
    session: requests.Session, entries: list[ChangelogEntry]
) -> tuple[list[tuple], list[str]]:
    """
    Downloads PR media for the gallery. Anything over the count or size limit is sent as a link.
    """
    media: list[tuple] = []
    links: list[str] = []
    total_size = 0

    for entry in entries:
        for url in iter_media_urls(entry):
            if len(media) >= DISCORD_MEDIA_GALLERY_MAX_ITEMS:
                links.append(url)
                continue

            downloaded = download_media(session, url)
            if not downloaded or total_size + len(downloaded[2]) > MEDIA_MAX_TOTAL_SIZE:
                links.append(url)
                continue

            ext, mime, data = downloaded
            total_size += len(data)
            media.append((f"changelog-media-{len(media) + 1}{ext}", mime, data, url, entry))

    return media, links

# endregion


# region Sending

def get_webhooks() -> list[tuple[str, str | None]]:
    """
    Webhook URL and the optional role pinged in its header. Only the Erida server gets a ping.
    """
    role_id = os.environ.get("DISCORD_CHANGELOG_ROLE_ERIDA", "").strip()
    webhooks = []
    for name, role in (("ERIDA", role_id if role_id.isdigit() else None), ("DEADSPACE", None)):
        url = os.environ.get(f"DISCORD_WEBHOOK_URL_{name}")
        if url:
            webhooks.append((url, role))

    if not webhooks:
        raise RuntimeError("No Discord webhooks configured!")

    return webhooks


def send_changelog(
    entries: list[ChangelogEntry],
    publish_context: dict[str, Any] | None,
) -> None:
    with requests.Session() as session:
        media, media_links = collect_media(session, entries)

    highlights = [e for e in entries if e.get("highlight")]
    regular = [e for e in entries if not e.get("highlight")]
    blocks = [
        *build_highlight_blocks(highlights, media),
        *build_section_blocks(regular),
        build_media_block([m for m in media if not m[4].get("highlight")])
        + build_footer(entries, media_links),
    ]

    for webhook_url, role_id in get_webhooks():
        messages = pack_messages(build_header(publish_context, role_id), blocks)
        for index, components in enumerate(messages, start=1):
            body: dict[str, Any] = {
                "components": components,
                "flags": DISCORD_COMPONENTS_V2_FLAG,
                # Only the header of the first message holds the role mention.
                "allowed_mentions": {"parse": [], "roles": [role_id] if role_id and index == 1 else []},
            }

            payload = json.dumps(components)
            attached = [m for m in media if f"attachment://{m[0]}\"" in payload]
            files = None
            if attached:
                body["attachments"] = [{"id": i, "filename": m[0]} for i, m in enumerate(attached)]
                files = [(f"files[{i}]", (m[0], m[2], m[1])) for i, m in enumerate(attached)]

            send_with_retry(webhook_url, body, f"Erida changelog {index}/{len(messages)}", files)
            if index != len(messages):
                time.sleep(DISCORD_MESSAGE_DELAY_SECONDS)


def with_components_url(webhook_url: str) -> str:
    parsed = urlparse(webhook_url)
    query = dict(parse_qsl(parsed.query))
    query["with_components"] = "true"
    return parsed._replace(query=urlencode(query)).geturl()


def send_with_retry(
    webhook_url: str,
    body: dict[str, Any],
    name: str,
    files: list | None = None,
) -> None:
    retry_attempt = 0
    max_retries = 30

    kwargs: dict[str, Any] = {"timeout": 60}
    if files:
        kwargs["data"] = {"payload_json": json.dumps(body)}
        kwargs["files"] = files
    else:
        kwargs["json"] = body

    while True:
        try:
            response = requests.post(with_components_url(webhook_url), **kwargs)
        except requests.exceptions.RequestException as e:
            retry_attempt += 1
            if retry_attempt > max_retries:
                raise RuntimeError(f"[{name}] Failed to send message") from e
            retry_after = min(30, 2 ** min(retry_attempt, 5))
            print(f"[{name}] Request failed, retrying after {retry_after} seconds")
            time.sleep(retry_after)
            continue

        if response.status_code in (429, 500, 502, 503, 504):
            retry_attempt += 1
            if retry_attempt > max_retries:
                raise RuntimeError(f"[{name}] Too many retries, giving up")
            retry_after = get_retry_after(response, retry_attempt)
            print(
                f"[{name}] Discord returned {response.status_code}, "
                f"retrying after {retry_after} seconds"
            )
            time.sleep(retry_after)
            continue

        if not response.ok:
            raise RuntimeError(
                f"[{name}] Discord rejected the message "
                f"({response.status_code}): {response.text}"
            )

        print(f"Sent to {name} webhook")
        break


def get_retry_after(response: requests.Response, retry_attempt: int) -> float:
    try:
        retry_after = float(response.json().get("retry_after", 0))
    except (AttributeError, ValueError, TypeError):
        retry_after = 0

    if retry_after <= 0:
        retry_after_header = response.headers.get("Retry-After")
        if retry_after_header:
            try:
                retry_after = float(retry_after_header)
            except ValueError:
                retry_after = 0

    if retry_after <= 0:
        retry_after = min(30, 2 ** min(retry_attempt, 5))

    return retry_after + 0.25

# endregion


def truncate(value: str, limit: int) -> str:
    if len(value) <= limit:
        return value

    return value[: limit - 6].rstrip() + " [...]"


if __name__ == "__main__":
    main()
