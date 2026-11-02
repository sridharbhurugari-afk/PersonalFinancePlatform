import { InjectionToken } from '@angular/core';

/** Base URL of the API gateway / dev proxy. Override in app.config for other environments. */
export const API_BASE_URL = new InjectionToken<string>('API_BASE_URL', {
  providedIn: 'root',
  factory: () => '/api',
});
