import * as LocalAuthentication from "expo-local-authentication";
import { useEffect, useMemo, useState } from "react";
import {
  ActivityIndicator,
  Alert,
  FlatList,
  KeyboardAvoidingView,
  Platform,
  Pressable,
  SafeAreaView,
  ScrollView,
  StatusBar,
  StyleSheet,
  Switch,
  Text,
  TextInput,
  View
} from "react-native";
import {
  apiFetch,
  cachedApiFetch,
  clearSession,
  getApiBaseUrl,
  getStoredSession,
  isBiometricEnabled,
  login,
  logout,
  normalizeApiBaseUrl,
  setBiometricEnabled,
  setSession,
  type JsonRecord,
  type MobileSession
} from "./src/api";

type TabKey =
  | "briefing"
  | "dashboard"
  | "chat"
  | "voice"
  | "tasks"
  | "approvals"
  | "notifications";

type ChatMessage = {
  role: "user" | "assistant";
  content: string;
  provider?: string | null;
  model?: string | null;
  fallback?: boolean;
};

type ChatResponse = {
  conversationId: string;
  reply: string;
  provider?: string | null;
  model?: string | null;
  usedFallback: boolean;
};

type VoiceSession = {
  id: string;
  status: string;
};

type VoiceConversationResponse = {
  transcript: string;
  reply: string;
  streamChunks: string[];
  speech: {
    audioContentBase64: string;
    format: string;
  };
};

const tabs: { key: TabKey; label: string }[] = [
  { key: "briefing", label: "Briefing" },
  { key: "dashboard", label: "Dashboard" },
  { key: "chat", label: "Chat" },
  { key: "voice", label: "Voice" },
  { key: "tasks", label: "Tasks" },
  { key: "approvals", label: "Approvals" },
  { key: "notifications", label: "Alerts" }
];

export default function App() {
  const [session, setActiveSession] = useState<MobileSession | null>(null);
  const [loading, setLoading] = useState(true);
  const [locked, setLocked] = useState(false);
  const [biometricEnabled, setBiometricEnabledState] = useState(false);
  const [activeTab, setActiveTab] = useState<TabKey>("briefing");

  useEffect(() => {
    void bootstrap();
  }, []);

  async function bootstrap() {
    const [storedSession, biometric] = await Promise.all([
      getStoredSession(),
      isBiometricEnabled()
    ]);

    setActiveSession(storedSession);
    setBiometricEnabledState(biometric);

    if (storedSession && biometric) {
      const authenticated = await promptBiometricUnlock();
      setLocked(!authenticated);
    }

    setLoading(false);
  }

  async function promptBiometricUnlock() {
    const supported = await LocalAuthentication.hasHardwareAsync();
    const enrolled = await LocalAuthentication.isEnrolledAsync();
    if (!supported || !enrolled) {
      return true;
    }

    const result = await LocalAuthentication.authenticateAsync({
      promptMessage: "Unlock Companion",
      fallbackLabel: "Use device passcode"
    });

    return result.success;
  }

  async function toggleBiometric(enabled: boolean) {
    if (enabled) {
      const supported = await LocalAuthentication.hasHardwareAsync();
      const enrolled = await LocalAuthentication.isEnrolledAsync();
      if (!supported || !enrolled) {
        Alert.alert("Biometrics unavailable", "This device does not have enrolled biometrics.");
        return;
      }
    }

    await setBiometricEnabled(enabled);
    setBiometricEnabledState(enabled);
  }

  async function signOut() {
    if (session) {
      try {
        await logout(session);
      } catch {
        // Local session cleanup still needs to happen when the API is unreachable.
      }
    }

    await clearSession();
    setActiveSession(null);
    setLocked(false);
  }

  if (loading) {
    return <LoadingScreen />;
  }

  if (!session) {
    return <LoginScreen onSession={setActiveSession} />;
  }

  if (locked) {
    return (
      <SafeAreaView style={styles.centerScreen}>
        <StatusBar barStyle="dark-content" />
        <Text style={styles.brand}>Companion Core</Text>
        <Text style={styles.muted}>Mobile session is locked.</Text>
        <Pressable
          style={styles.primaryButton}
          onPress={async () => setLocked(!(await promptBiometricUnlock()))}
        >
          <Text style={styles.primaryButtonText}>Unlock</Text>
        </Pressable>
      </SafeAreaView>
    );
  }

  return (
    <SafeAreaView style={styles.screen}>
      <StatusBar barStyle="dark-content" />
      <View style={styles.header}>
        <View>
          <Text style={styles.eyebrow}>Companion Core</Text>
          <Text style={styles.title}>{session.me.profile.displayName}</Text>
        </View>
        <Pressable style={styles.secondaryButton} onPress={signOut}>
          <Text style={styles.secondaryButtonText}>Log out</Text>
        </Pressable>
      </View>

      <View style={styles.preferenceRow}>
        <Text style={styles.preferenceLabel}>Biometric lock</Text>
        <Switch value={biometricEnabled} onValueChange={toggleBiometric} />
      </View>

      <ScrollView horizontal showsHorizontalScrollIndicator={false} style={styles.tabs}>
        {tabs.map((tab) => (
          <Pressable
            key={tab.key}
            style={[styles.tab, activeTab === tab.key && styles.activeTab]}
            onPress={() => setActiveTab(tab.key)}
          >
            <Text style={[styles.tabText, activeTab === tab.key && styles.activeTabText]}>
              {tab.label}
            </Text>
          </Pressable>
        ))}
      </ScrollView>

      <View style={styles.content}>
        {activeTab === "briefing" ? <JsonDataScreen session={session} endpoint="/api/companion/briefing" cacheKey="mobile.briefing" /> : null}
        {activeTab === "dashboard" ? <JsonDataScreen session={session} endpoint="/api/companion/dashboard" cacheKey="mobile.dashboard" /> : null}
        {activeTab === "chat" ? <ChatScreen session={session} /> : null}
        {activeTab === "voice" ? <VoiceScreen session={session} /> : null}
        {activeTab === "tasks" ? <ListScreen session={session} title="Tasks" endpoint="/api/tasks/open" cacheKey="mobile.tasks" /> : null}
        {activeTab === "approvals" ? <ListScreen session={session} title="Approvals" endpoint="/api/approvals" cacheKey="mobile.approvals" /> : null}
        {activeTab === "notifications" ? <ListScreen session={session} title="Notifications" endpoint="/api/notifications?includeRead=false" cacheKey="mobile.notifications" /> : null}
      </View>
    </SafeAreaView>
  );
}

function LoadingScreen() {
  return (
    <SafeAreaView style={styles.centerScreen}>
      <ActivityIndicator />
      <Text style={styles.muted}>Loading Companion</Text>
    </SafeAreaView>
  );
}

function LoginScreen({ onSession }: { onSession: (session: MobileSession) => void }) {
  const [apiBaseUrl, setApiBaseUrlInput] = useState("");
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    void getApiBaseUrl().then(setApiBaseUrlInput);
  }, []);

  async function submit() {
    setBusy(true);
    setError(null);
    try {
      const normalizedBaseUrl = normalizeApiBaseUrl(apiBaseUrl);
      if (!normalizedBaseUrl) {
        setError("Enter the Companion API URL.");
        return;
      }

      const result = await login(normalizedBaseUrl, email.trim(), password);
      await setSession(result.accessToken, result.me, normalizedBaseUrl);
      onSession({
        accessToken: result.accessToken,
        me: result.me,
        apiBaseUrl: normalizedBaseUrl
      });
    } catch (error) {
      setError(error instanceof Error ? error.message : "Login failed");
    } finally {
      setBusy(false);
    }
  }

  return (
    <KeyboardAvoidingView
      behavior={Platform.OS === "ios" ? "padding" : undefined}
      style={styles.loginScreen}
    >
      <Text style={styles.brand}>Companion Core</Text>
      <Text style={styles.loginCopy}>Use your Companion API URL from the same LAN or Tailscale network.</Text>
      <TextInput
        autoCapitalize="none"
        value={apiBaseUrl}
        onChangeText={setApiBaseUrlInput}
        style={styles.input}
        placeholder="API base URL"
      />
      <TextInput
        autoCapitalize="none"
        keyboardType="email-address"
        value={email}
        onChangeText={setEmail}
        style={styles.input}
        placeholder="Email"
      />
      <TextInput
        value={password}
        onChangeText={setPassword}
        style={styles.input}
        placeholder="Password"
        secureTextEntry
      />
      {error ? <Text style={styles.errorText}>{error}</Text> : null}
      <Pressable
        disabled={busy || !apiBaseUrl.trim() || !email.trim() || !password}
        style={[styles.primaryButton, (busy || !apiBaseUrl.trim() || !email.trim() || !password) && styles.disabledButton]}
        onPress={submit}
      >
        <Text style={styles.primaryButtonText}>{busy ? "Signing in" : "Sign in"}</Text>
      </Pressable>
    </KeyboardAvoidingView>
  );
}

function JsonDataScreen({
  session,
  endpoint,
  cacheKey
}: {
  session: MobileSession;
  endpoint: string;
  cacheKey: string;
}) {
  const { data, loading, fromCache, refresh } = useCachedEndpoint<JsonRecord>(session, endpoint, cacheKey);

  return (
    <ScrollView style={styles.panel}>
      <ScreenHeader title={endpoint.includes("briefing") ? "Briefing" : "Dashboard"} fromCache={fromCache} onRefresh={refresh} />
      {loading ? (
        <View style={styles.centerPanel}>
          <ActivityIndicator />
          <Text style={styles.muted}>Loading {endpoint.includes("briefing") ? "briefing" : "dashboard"}</Text>
        </View>
      ) : (
        <JsonBlock data={data} />
      )}
    </ScrollView>
  );
}

function ListScreen({
  session,
  title,
  endpoint,
  cacheKey
}: {
  session: MobileSession;
  title: string;
  endpoint: string;
  cacheKey: string;
}) {
  const { data, loading, fromCache, refresh } = useCachedEndpoint<JsonRecord[]>(session, endpoint, cacheKey, []);
  const items = data ?? [];

  return (
    <View style={styles.panel}>
      <ScreenHeader title={title} fromCache={fromCache} onRefresh={refresh} />
      {loading ? (
        <ActivityIndicator />
      ) : (
        <FlatList
          data={items}
          keyExtractor={(item, index) => String(item.id ?? index)}
          ListEmptyComponent={<Text style={styles.muted}>No {title.toLowerCase()} yet.</Text>}
          renderItem={({ item }) => (
            <View style={styles.listItem}>
              <Text style={styles.itemTitle}>
                {String(item.title ?? item.reason ?? item.subject ?? item.type ?? "Untitled")}
              </Text>
              <Text style={styles.itemMeta}>
                {String(item.status ?? item.priority ?? item.severity ?? "Open")}
              </Text>
              <Text numberOfLines={3} style={styles.itemBody}>
                {String(item.description ?? item.body ?? item.payload ?? item.reason ?? "")}
              </Text>
            </View>
          )}
        />
      )}
    </View>
  );
}

function ChatScreen({ session }: { session: MobileSession }) {
  const [conversationId, setConversationId] = useState<string | null>(null);
  const [input, setInput] = useState("");
  const [busy, setBusy] = useState(false);
  const [messages, setMessages] = useState<ChatMessage[]>([]);
  const [error, setError] = useState<string | null>(null);

  async function send() {
    const message = input.trim();
    if (!message || busy) {
      return;
    }

    setInput("");
    setError(null);
    setMessages((current) => [...current, { role: "user", content: message }]);
    setBusy(true);

    try {
      const response = await apiFetch<ChatResponse>(session, "/api/chat", {
        method: "POST",
        body: JSON.stringify({ message, conversationId })
      });

      setConversationId(response.conversationId);
      setMessages((current) => [
        ...current,
        {
          role: "assistant",
          content: response.reply,
          provider: response.provider,
          model: response.model,
          fallback: response.usedFallback
        }
      ]);
    } catch (error) {
      setError(error instanceof Error ? error.message : "Chat failed");
    } finally {
      setBusy(false);
    }
  }

  return (
    <KeyboardAvoidingView behavior={Platform.OS === "ios" ? "padding" : undefined} style={styles.panel}>
      <FlatList
        data={messages}
        keyExtractor={(_, index) => String(index)}
        ListHeaderComponent={<ScreenHeader title="Chat" />}
        ListEmptyComponent={<Text style={styles.muted}>Ask Companion for help with the day.</Text>}
        ListFooterComponent={
          busy ? (
            <View style={[styles.chatBubble, styles.assistantBubble]}>
              <Text style={styles.assistantText}>Companion is thinking...</Text>
            </View>
          ) : null
        }
        renderItem={({ item }) => (
          <View style={[styles.chatBubble, item.role === "user" ? styles.userBubble : styles.assistantBubble]}>
            <Text style={item.role === "user" ? styles.userText : styles.assistantText}>{item.content}</Text>
            {item.role === "assistant" && (item.provider || item.fallback) ? (
              <Text style={styles.itemMeta}>
                {[item.provider, item.model, item.fallback ? "fallback" : null].filter(Boolean).join(" / ")}
              </Text>
            ) : null}
          </View>
        )}
      />
      {error ? <Text style={styles.errorText}>{error}</Text> : null}
      <View style={styles.composer}>
        <TextInput
          multiline
          value={input}
          onChangeText={setInput}
          style={[styles.input, styles.composerInput]}
          placeholder="Message Companion"
        />
        <Pressable disabled={busy || !input.trim()} style={styles.primaryButton} onPress={send}>
          <Text style={styles.primaryButtonText}>{busy ? "..." : "Send"}</Text>
        </Pressable>
      </View>
    </KeyboardAvoidingView>
  );
}

function VoiceScreen({ session }: { session: MobileSession }) {
  const [voiceSession, setVoiceSession] = useState<VoiceSession | null>(null);
  const [utterance, setUtterance] = useState("What should I focus on today?");
  const [result, setResult] = useState<VoiceConversationResponse | null>(null);
  const [busy, setBusy] = useState(false);

  async function startSession() {
    setBusy(true);
    try {
      const response = await apiFetch<VoiceSession>(session, "/api/voice/sessions", {
        method: "POST",
        body: JSON.stringify({
          speechToTextProvider: "LocalWhisper",
          textToSpeechProvider: "LocalPiper"
        })
      });
      setVoiceSession(response);
    } catch (error) {
      Alert.alert("Voice session failed", error instanceof Error ? error.message : "Unknown error");
    } finally {
      setBusy(false);
    }
  }

  async function converse() {
    const active = voiceSession;
    if (!active) {
      return;
    }

    setBusy(true);
    try {
      const response = await apiFetch<VoiceConversationResponse>(
        session,
        `/api/voice/sessions/${active.id}/conversation`,
        {
          method: "POST",
          body: JSON.stringify({ simulatedTranscript: utterance })
        }
      );
      setResult(response);
    } catch (error) {
      Alert.alert("Voice conversation failed", error instanceof Error ? error.message : "Unknown error");
    } finally {
      setBusy(false);
    }
  }

  return (
    <ScrollView style={styles.panel}>
      <ScreenHeader title="Voice" />
      <Text style={styles.muted}>Mobile voice is wired to the backend voice session API with simulated transcript input for now.</Text>
      <Pressable disabled={busy || !!voiceSession} style={styles.primaryButton} onPress={startSession}>
        <Text style={styles.primaryButtonText}>{voiceSession ? "Session active" : "Start voice session"}</Text>
      </Pressable>
      <TextInput
        multiline
        value={utterance}
        onChangeText={setUtterance}
        style={[styles.input, styles.voiceInput]}
        placeholder="Simulated voice transcript"
      />
      <Pressable disabled={busy || !voiceSession || !utterance.trim()} style={styles.primaryButton} onPress={converse}>
        <Text style={styles.primaryButtonText}>{busy ? "Listening" : "Send voice turn"}</Text>
      </Pressable>
      {result ? (
        <View style={styles.listItem}>
          <Text style={styles.itemTitle}>Transcript</Text>
          <Text style={styles.itemBody}>{result.transcript}</Text>
          <Text style={styles.itemTitle}>Reply</Text>
          <Text style={styles.itemBody}>{result.reply}</Text>
          <Text style={styles.itemMeta}>
            {result.streamChunks.length} chunks / {result.speech.format}
          </Text>
        </View>
      ) : null}
    </ScrollView>
  );
}

function ScreenHeader({
  title,
  fromCache,
  onRefresh
}: {
  title: string;
  fromCache?: boolean;
  onRefresh?: () => void;
}) {
  return (
    <View style={styles.screenHeader}>
      <View>
        <Text style={styles.sectionTitle}>{title}</Text>
        {fromCache ? <Text style={styles.cacheNotice}>Offline cache</Text> : null}
      </View>
      {onRefresh ? (
        <Pressable style={styles.secondaryButton} onPress={onRefresh}>
          <Text style={styles.secondaryButtonText}>Refresh</Text>
        </Pressable>
      ) : null}
    </View>
  );
}

function JsonBlock({ data }: { data: unknown }) {
  return (
    <Text style={styles.jsonText}>
      {JSON.stringify(data ?? {}, null, 2)}
    </Text>
  );
}

function useCachedEndpoint<T>(
  session: MobileSession,
  endpoint: string,
  cacheKey: string,
  fallback?: T
) {
  const [data, setData] = useState<T | undefined>(fallback);
  const [loading, setLoading] = useState(true);
  const [fromCache, setFromCache] = useState(false);

  const refresh = useMemo(
    () => async () => {
      setLoading(true);
      try {
        const result = await cachedApiFetch<T>(session, endpoint, cacheKey);
        setData(result.data);
        setFromCache(result.fromCache);
      } catch (error) {
        Alert.alert("Load failed", error instanceof Error ? error.message : "Unknown error");
      } finally {
        setLoading(false);
      }
    },
    [cacheKey, endpoint, session]
  );

  useEffect(() => {
    void refresh();
  }, [refresh]);

  return { data, loading, fromCache, refresh };
}

const colors = {
  bg: "#f6f7f9",
  panel: "#ffffff",
  ink: "#18202f",
  muted: "#667085",
  line: "#d8dee8",
  accent: "#2f6fed",
  accentSoft: "#e7efff",
  danger: "#ad2f45"
};

const styles = StyleSheet.create({
  screen: {
    flex: 1,
    backgroundColor: colors.bg
  },
  centerScreen: {
    flex: 1,
    alignItems: "center",
    justifyContent: "center",
    gap: 16,
    backgroundColor: colors.bg,
    padding: 24
  },
  loginScreen: {
    flex: 1,
    justifyContent: "center",
    gap: 14,
    backgroundColor: colors.bg,
    padding: 24
  },
  brand: {
    fontSize: 28,
    fontWeight: "800",
    color: colors.ink
  },
  loginCopy: {
    color: colors.muted,
    fontSize: 15,
    lineHeight: 22,
    marginBottom: 8
  },
  header: {
    flexDirection: "row",
    justifyContent: "space-between",
    alignItems: "center",
    paddingHorizontal: 16,
    paddingTop: 12,
    paddingBottom: 10
  },
  eyebrow: {
    color: colors.muted,
    fontSize: 12,
    fontWeight: "700",
    textTransform: "uppercase"
  },
  title: {
    color: colors.ink,
    fontSize: 22,
    fontWeight: "800"
  },
  preferenceRow: {
    marginHorizontal: 16,
    marginBottom: 10,
    padding: 12,
    borderRadius: 8,
    backgroundColor: colors.panel,
    borderWidth: StyleSheet.hairlineWidth,
    borderColor: colors.line,
    flexDirection: "row",
    justifyContent: "space-between",
    alignItems: "center"
  },
  preferenceLabel: {
    color: colors.ink,
    fontWeight: "700"
  },
  tabs: {
    maxHeight: 46,
    paddingHorizontal: 12
  },
  tab: {
    height: 36,
    justifyContent: "center",
    paddingHorizontal: 14,
    marginHorizontal: 4,
    borderRadius: 8,
    backgroundColor: colors.panel,
    borderWidth: StyleSheet.hairlineWidth,
    borderColor: colors.line
  },
  activeTab: {
    backgroundColor: colors.accent,
    borderColor: colors.accent
  },
  tabText: {
    color: colors.muted,
    fontWeight: "700"
  },
  activeTabText: {
    color: "#ffffff"
  },
  content: {
    flex: 1,
    padding: 16
  },
  panel: {
    flex: 1,
    borderRadius: 8,
    backgroundColor: colors.panel,
    borderWidth: StyleSheet.hairlineWidth,
    borderColor: colors.line,
    padding: 14
  },
  centerPanel: {
    alignItems: "center",
    justifyContent: "center",
    gap: 10,
    paddingVertical: 28
  },
  screenHeader: {
    flexDirection: "row",
    justifyContent: "space-between",
    alignItems: "center",
    marginBottom: 14
  },
  sectionTitle: {
    color: colors.ink,
    fontSize: 18,
    fontWeight: "800"
  },
  cacheNotice: {
    color: colors.danger,
    fontSize: 12,
    fontWeight: "700"
  },
  input: {
    minHeight: 46,
    borderRadius: 8,
    borderWidth: 1,
    borderColor: colors.line,
    backgroundColor: "#ffffff",
    color: colors.ink,
    paddingHorizontal: 12,
    paddingVertical: 10
  },
  primaryButton: {
    minHeight: 46,
    alignItems: "center",
    justifyContent: "center",
    borderRadius: 8,
    backgroundColor: colors.accent,
    paddingHorizontal: 16,
    marginTop: 10
  },
  primaryButtonText: {
    color: "#ffffff",
    fontWeight: "800"
  },
  secondaryButton: {
    minHeight: 38,
    alignItems: "center",
    justifyContent: "center",
    borderRadius: 8,
    borderWidth: 1,
    borderColor: colors.line,
    backgroundColor: colors.panel,
    paddingHorizontal: 12
  },
  secondaryButtonText: {
    color: colors.ink,
    fontWeight: "700"
  },
  disabledButton: {
    opacity: 0.55
  },
  muted: {
    color: colors.muted,
    lineHeight: 20
  },
  errorText: {
    color: colors.danger,
    fontSize: 13,
    fontWeight: "700",
    lineHeight: 18
  },
  jsonText: {
    color: colors.ink,
    fontFamily: Platform.select({ ios: "Menlo", android: "monospace", default: "monospace" }),
    fontSize: 12,
    lineHeight: 18
  },
  listItem: {
    borderRadius: 8,
    borderWidth: StyleSheet.hairlineWidth,
    borderColor: colors.line,
    backgroundColor: "#fbfcfe",
    padding: 12,
    marginBottom: 10
  },
  itemTitle: {
    color: colors.ink,
    fontSize: 15,
    fontWeight: "800",
    marginBottom: 4
  },
  itemMeta: {
    color: colors.accent,
    fontSize: 12,
    fontWeight: "800",
    marginBottom: 6
  },
  itemBody: {
    color: colors.muted,
    lineHeight: 20
  },
  composer: {
    gap: 10,
    paddingTop: 10
  },
  composerInput: {
    minHeight: 72,
    textAlignVertical: "top"
  },
  chatBubble: {
    borderRadius: 8,
    padding: 12,
    marginBottom: 10,
    maxWidth: "92%"
  },
  userBubble: {
    alignSelf: "flex-end",
    backgroundColor: colors.accent
  },
  assistantBubble: {
    alignSelf: "flex-start",
    backgroundColor: colors.accentSoft
  },
  userText: {
    color: "#ffffff",
    lineHeight: 20
  },
  assistantText: {
    color: colors.ink,
    lineHeight: 20
  },
  voiceInput: {
    minHeight: 86,
    marginTop: 12,
    textAlignVertical: "top"
  }
});
