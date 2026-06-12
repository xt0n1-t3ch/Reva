import argparse
import csv
import json
import re
from pathlib import Path

TEXT_EXTENSIONS = {".txt", ".md"}
CSV_EXTENSIONS = {".csv"}
BINARY_TEXT_EXTENSIONS = {".pdf", ".png", ".jpg", ".jpeg", ".tif", ".tiff"}


def parse_document(path: Path) -> dict:
    extension = path.suffix.lower()
    if extension in TEXT_EXTENSIONS:
        text = path.read_text(encoding="utf-8-sig")
        return build_payload("fallback-text", extension.removeprefix("."), text, text, [], [])

    if extension in CSV_EXTENSIONS:
        return parse_csv(path)

    docling_payload = try_docling(path)
    if docling_payload is not None:
        return docling_payload

    if extension in BINARY_TEXT_EXTENSIONS:
        text = extract_visible_binary_text(path)
        warnings = ["Docling is not installed; binary document used fallback visible-text extraction."]
        return build_payload("fallback-binary-text", extension.removeprefix("."), text, text, [], warnings)

    raise ValueError(f"Unsupported extension: {extension}")


def parse_csv(path: Path) -> dict:
    with path.open("r", encoding="utf-8-sig", newline="") as handle:
        reader = csv.DictReader(handle)
        rows = [{key or "Column": value or "" for key, value in row.items()} for row in reader]
        headers = reader.fieldnames or []

    markdown = csv_to_markdown(headers, rows)
    text = path.read_text(encoding="utf-8-sig")
    tables = [{"name": path.stem, "headers": headers, "rows": rows}]
    return build_payload("fallback-csv", "csv", text, markdown, tables, [])


def try_docling(path: Path) -> dict | None:
    try:
        from docling.document_converter import DocumentConverter
    except ImportError:
        return None

    converter = DocumentConverter()
    result = converter.convert(str(path))
    document = result.document
    markdown = document.export_to_markdown()
    text = re.sub(r"\s+", " ", markdown).strip()
    return build_payload("docling", path.suffix.lower().removeprefix("."), text, markdown, [], [])


def extract_visible_binary_text(path: Path) -> str:
    data = path.read_bytes()
    decoded = data.decode("latin-1", errors="ignore")
    tokens = re.findall(r"[A-Za-z0-9][A-Za-z0-9 .,:;/%$#_\-]{2,}", decoded)
    return "\n".join(token.strip() for token in tokens if token.strip())


def csv_to_markdown(headers: list[str], rows: list[dict[str, str]]) -> str:
    if not headers:
        return ""

    lines = ["| " + " | ".join(headers) + " |", "| " + " | ".join("---" for _ in headers) + " |"]
    for row in rows:
        lines.append("| " + " | ".join(row.get(header, "") for header in headers) + " |")
    return "\n".join(lines)


def build_payload(parser_profile: str, source_format: str, text: str, markdown: str, tables: list[dict], warnings: list[str]) -> dict:
    return {
        "parserProfile": parser_profile,
        "sourceFormat": source_format,
        "text": text,
        "markdown": markdown,
        "tables": tables,
        "warnings": warnings,
    }


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("--input", required=True)
    args = parser.parse_args()
    payload = parse_document(Path(args.input))
    print(json.dumps(payload, ensure_ascii=False))


if __name__ == "__main__":
    main()
