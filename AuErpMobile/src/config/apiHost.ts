import {Platform} from 'react-native';

/**
 * Optional: set to your PC's IPv4 (from `ipconfig`) when using a physical device
 * on Wi‑Fi without `adb reverse tcp:5242 tcp:5242`.
 * Example: '192.168.1.50'
 */
export const DEV_HOST_OVERRIDE: string | null = null;

const API_PORT = 5242;

function isAndroidEmulator(): boolean {
  if (Platform.OS !== 'android') return false;
  const c = Platform.constants as {
    Model?: string;
    Brand?: string;
    Manufacturer?: string;
    Fingerprint?: string;
  };
  const model = (c.Model ?? '').toLowerCase();
  const fingerprint = (c.Fingerprint ?? '').toLowerCase();
  return (
    model.includes('sdk') ||
    model.includes('emulator') ||
    fingerprint.includes('generic') ||
    fingerprint.includes('sdk_gphone') ||
    (c.Brand === 'google' && c.Manufacturer === 'Google' && model.includes('sdk'))
  );
}

/** Hostname the app uses to reach AU_ERP on your dev machine. */
export function getDevApiHost(): string {
  if (DEV_HOST_OVERRIDE) return DEV_HOST_OVERRIDE;
  if (Platform.OS === 'android') {
    return isAndroidEmulator() ? '10.0.2.2' : '127.0.0.1';
  }
  return 'localhost';
}

export function getApiBaseUrl(): string {
  return `http://${getDevApiHost()}:${API_PORT}/api/mobile`;
}
