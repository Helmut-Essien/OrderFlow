export const ACCESS_TOKEN_SKEW_MS = 30_000;

/**
 * Reads JWT `exp` without verifying the signature. Used only to drop expired sessions on the client.
 * @returns UTC epoch milliseconds, or null when the token is malformed.
 */
export function readJwtExpiryMs(token: string): number | null {
  try {
    const payloadPart = token.split('.')[1];
    if (!payloadPart) {
      return null;
    }

    const padded = payloadPart.replace(/-/g, '+').replace(/_/g, '/');
    const payload = JSON.parse(atob(padded)) as { exp?: number };
    return typeof payload.exp === 'number' ? payload.exp * 1000 : null;
  } catch {
    return null;
  }
}

/**
 * True when the access token is missing, unreadable, or within `skewMs` of expiry.
 * Default skew is 30s so we log out before the API starts returning 401.
 */
export function isAccessTokenExpired(
  token: string | null,
  nowMs = Date.now(),
  skewMs = ACCESS_TOKEN_SKEW_MS
): boolean {
  if (!token) {
    return true;
  }

  const expiryMs = readJwtExpiryMs(token);
  if (expiryMs == null) {
    return true;
  }

  return nowMs + skewMs >= expiryMs;
}
