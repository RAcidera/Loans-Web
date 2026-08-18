/** Diary entries carry Tags as one comma-separated string on the wire (see DiaryEntry.tags's doc comment) — these two helpers are the single place that splits/joins it for every chip-input and chip-list in the app. */
export function parseTags(csv: string | undefined | null): string[] {
  if (!csv) return [];
  return csv
    .split(',')
    .map((t) => t.trim())
    .filter((t) => t.length > 0);
}

export function joinTags(tags: string[]): string {
  return tags.join(',');
}
