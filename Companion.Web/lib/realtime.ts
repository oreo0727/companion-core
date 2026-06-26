export type RealtimeEventHandler<TPayload = unknown> = (payload: TPayload) => void;

export interface RealtimeClient {
  connect(): Promise<void>;
  disconnect(): Promise<void>;
  subscribe<TPayload>(eventName: string, handler: RealtimeEventHandler<TPayload>): () => void;
}

export class SignalRReadyClient implements RealtimeClient {
  private handlers = new Map<string, Set<RealtimeEventHandler>>();

  async connect() {
    return Promise.resolve();
  }

  async disconnect() {
    this.handlers.clear();
  }

  subscribe<TPayload>(eventName: string, handler: RealtimeEventHandler<TPayload>) {
    const existing = this.handlers.get(eventName) ?? new Set<RealtimeEventHandler>();
    existing.add(handler as RealtimeEventHandler);
    this.handlers.set(eventName, existing);

    return () => {
      existing.delete(handler as RealtimeEventHandler);
    };
  }
}
