import AsyncStorage from "@react-native-async-storage/async-storage";

const tokenKey = "companion.mobile.accessToken";
const userKey = "companion.mobile.currentUser";
const apiBaseUrlKey = "companion.mobile.apiBaseUrl";
const biometricKey = "companion.mobile.biometricEnabled";

const defaultApiBaseUrl =
  process.env.EXPO_PUBLIC_API_BASE_URL?.replace(/\/$/, "") ?? "http://localhost:8080";

export type JsonRecord = Record<string, unknown>;

export type CurrentUser = {
  profile: {
    userId: string;
    userProfileId: string;
    email: string;
    displayName: string;
    roles: string[];
  };
  preferences: unknown[];
  capabilities: JsonRecord;
};

export type MobileSession = {
  accessToken: string;
  me: CurrentUser;
  apiBaseUrl: string;
};

export async function getApiBaseUrl() {
  return (await AsyncStorage.getItem(apiBaseUrlKey)) ?? defaultApiBaseUrl;
}

export async function setApiBaseUrl(value: string) {
  await AsyncStorage.setItem(apiBaseUrlKey, value.replace(/\/$/, ""));
}

export async function getStoredSession(): Promise<MobileSession | null> {
  const [accessToken, rawUser, apiBaseUrl] = await Promise.all([
    AsyncStorage.getItem(tokenKey),
    AsyncStorage.getItem(userKey),
    getApiBaseUrl()
  ]);

  if (!accessToken || !rawUser) {
    return null;
  }

  try {
    return {
      accessToken,
      me: JSON.parse(rawUser) as CurrentUser,
      apiBaseUrl
    };
  } catch {
    await clearSession();
    return null;
  }
}

export async function setSession(accessToken: string, me: CurrentUser, apiBaseUrl: string) {
  await Promise.all([
    AsyncStorage.setItem(tokenKey, accessToken),
    AsyncStorage.setItem(userKey, JSON.stringify(me)),
    setApiBaseUrl(apiBaseUrl)
  ]);
}

export async function clearSession() {
  await Promise.all([
    AsyncStorage.removeItem(tokenKey),
    AsyncStorage.removeItem(userKey)
  ]);
}

export async function isBiometricEnabled() {
  return (await AsyncStorage.getItem(biometricKey)) === "true";
}

export async function setBiometricEnabled(enabled: boolean) {
  await AsyncStorage.setItem(biometricKey, enabled ? "true" : "false");
}

export async function login(apiBaseUrl: string, email: string, password: string) {
  const response = await fetch(`${apiBaseUrl.replace(/\/$/, "")}/api/auth/login`, {
    method: "POST",
    headers: {
      Accept: "application/json",
      "Content-Type": "application/json"
    },
    body: JSON.stringify({ email, password })
  });

  if (!response.ok) {
    throw new Error((await response.text()) || "Login failed");
  }

  return (await response.json()) as {
    accessToken: string;
    expiresUtc: string;
    me: CurrentUser;
  };
}

export async function apiFetch<T>(
  session: MobileSession,
  path: string,
  init: RequestInit = {}
): Promise<T> {
  const headers = new Headers(init.headers);
  headers.set("Accept", "application/json");

  if (init.body && !headers.has("Content-Type")) {
    headers.set("Content-Type", "application/json");
  }

  headers.set("Authorization", `Bearer ${session.accessToken}`);

  const response = await fetch(`${session.apiBaseUrl}${path}`, {
    ...init,
    headers
  });

  if (response.status === 401) {
    await clearSession();
  }

  if (!response.ok) {
    throw new Error((await response.text()) || `${response.status} ${response.statusText}`);
  }

  if (response.status === 204) {
    return undefined as T;
  }

  return (await response.json()) as T;
}

export async function cachedApiFetch<T>(
  session: MobileSession,
  path: string,
  cacheKey: string
): Promise<{ data: T; fromCache: boolean }> {
  try {
    const data = await apiFetch<T>(session, path);
    await AsyncStorage.setItem(cacheKey, JSON.stringify(data));
    return { data, fromCache: false };
  } catch (error) {
    const cached = await AsyncStorage.getItem(cacheKey);
    if (!cached) {
      throw error;
    }

    return {
      data: JSON.parse(cached) as T,
      fromCache: true
    };
  }
}
