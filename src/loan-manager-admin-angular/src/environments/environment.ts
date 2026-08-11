export const environment = {
  production: false,
  // .NET API runs on http://localhost:5080 by default (see backend's
  // launchSettings.json). Angular's dev server runs on :4200, so this must
  // be an absolute URL, not a relative '/api' path — a relative path would
  // resolve against the Angular dev server's own origin instead.
  apiBaseUrl: 'http://localhost:5080/api',
};
