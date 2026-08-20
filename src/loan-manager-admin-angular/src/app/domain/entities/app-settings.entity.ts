// Domain layer — mirrors the backend's General Settings area (AppSettingsDto).

export interface AppSettings {
  /** IANA timezone id (e.g. "Asia/Manila") — the single source of truth for every "today"/business-local calculation, set by an Admin under Settings. */
  businessTimeZoneId: string;
  /** Today's date in the Business Time Zone, computed server-side ("yyyy-MM-dd") — use this instead of `new Date()` for "today" grouping/highlighting, since the browser's own clock/timezone may not match the business's. */
  currentBusinessDate: string;
}
