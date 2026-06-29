"use client";

import clsx from "clsx";
import {
  Activity,
  Bell,
  Bot,
  CalendarDays,
  CheckSquare,
  ChevronRight,
  ClipboardCheck,
  Database,
  FolderKanban,
  Goal,
  Home,
  Inbox,
  KeyRound,
  LogOut,
  Mail,
  MemoryStick,
  MessageSquare,
  MonitorCog,
  Moon,
  Plug,
  Search,
  Settings,
  ShieldCheck,
  Sun,
  Wrench
} from "lucide-react";
import Link from "next/link";
import { usePathname, useRouter } from "next/navigation";
import { useEffect, useMemo, useState } from "react";
import type { ReactNode } from "react";
import { clearSession, getStoredUser, getToken, type CurrentUser } from "@/lib/api";

const navItems = [
  { href: "/dashboard", label: "Dashboard", icon: Home },
  { href: "/chat", label: "Chat", icon: MessageSquare },
  { href: "/memories", label: "Memories", icon: MemoryStick },
  { href: "/knowledge", label: "Knowledge", icon: Database },
  { href: "/tasks", label: "Tasks", icon: CheckSquare },
  { href: "/goals", label: "Goals", icon: Goal },
  { href: "/projects", label: "Projects", icon: FolderKanban },
  { href: "/open-loops", label: "Open Loops", icon: Activity },
  { href: "/calendar", label: "Calendar", icon: CalendarDays },
  { href: "/email", label: "Email", icon: Mail },
  { href: "/notifications", label: "Notifications", icon: Bell },
  { href: "/approvals", label: "Approvals", icon: ClipboardCheck },
  { href: "/tool-executions", label: "Tool Executions", icon: Wrench },
  { href: "/audit", label: "Audit", icon: ShieldCheck },
  { href: "/admin-health", label: "Health", icon: MonitorCog },
  { href: "/connectors", label: "Connectors", icon: Plug },
  { href: "/settings", label: "Settings", icon: Settings },
  { href: "/ai-settings", label: "AI Settings", icon: KeyRound }
];

export function AppShell({ children }: { children: ReactNode }) {
  const pathname = usePathname();
  const router = useRouter();
  const [user, setUser] = useState<CurrentUser | null>(null);
  const [ready, setReady] = useState(false);
  const [dark, setDark] = useState(false);
  const publicPaths = pathname === "/login" || pathname === "/setup";

  useEffect(() => {
    const token = getToken();
    const storedUser = getStoredUser<CurrentUser>();

    if (!token && !publicPaths) {
      router.replace("/login");
      return;
    }

    setUser(storedUser);
    setReady(true);
    setDark(document.documentElement.classList.contains("dark"));
  }, [pathname, publicPaths, router]);

  const activeTitle = useMemo(
    () => navItems.find((item) => item.href === pathname)?.label ?? "Companion",
    [pathname]
  );

  function toggleTheme() {
    const next = !dark;
    setDark(next);
    document.documentElement.classList.toggle("dark", next);
    window.localStorage.setItem("companion.theme", next ? "dark" : "light");
  }

  function logout() {
    clearSession();
    router.replace("/login");
  }

  if (publicPaths) {
    return <>{children}</>;
  }

  if (!ready) {
    return (
      <main className="flex min-h-screen items-center justify-center bg-surface text-ink">
        <Bot className="h-6 w-6 animate-pulse text-accent" />
      </main>
    );
  }

  return (
    <div className="min-h-screen bg-surface/80 text-ink">
      <aside className="fixed inset-y-0 left-0 z-20 hidden w-72 border-r border-line/80 bg-surface-raised/90 backdrop-blur-xl lg:block">
        <div className="flex h-16 items-center gap-3 border-b border-line/80 px-5">
          <div className="flex h-9 w-9 items-center justify-center rounded-md border border-accent/40 bg-accent/15 text-accent shadow-[0_0_24px_rgb(var(--panel-glow)/0.22)]">
            <Bot className="h-5 w-5" />
          </div>
          <div>
            <div className="text-sm font-semibold">Companion Core</div>
            <div className="text-xs text-ink-muted">Operations Console</div>
          </div>
        </div>
        <nav className="h-[calc(100vh-4rem)] overflow-y-auto p-3">
          {navItems.map((item) => {
            const Icon = item.icon;
            const active = pathname === item.href;
            return (
              <Link
                key={item.href}
                href={item.href}
                className={clsx(
                  "mb-1 flex h-10 items-center gap-3 rounded-md px-3 text-sm transition",
                  active
                    ? "border border-accent/30 bg-accent/15 text-accent shadow-[inset_0_0_0_1px_rgb(var(--accent)/0.12)]"
                    : "text-ink-muted hover:bg-surface-muted/80 hover:text-ink"
                )}
              >
                <Icon className="h-4 w-4" />
                <span>{item.label}</span>
              </Link>
            );
          })}
        </nav>
      </aside>

      <div className="lg:pl-72">
        <header className="sticky top-0 z-10 border-b border-line/80 bg-surface-raised/80 backdrop-blur-xl">
          <div className="flex min-h-16 items-center justify-between gap-3 px-4 sm:px-6">
            <div className="min-w-0">
              <div className="flex items-center gap-2 text-xs text-ink-muted">
                <span>Companion</span>
                <ChevronRight className="h-3 w-3" />
                <span>{activeTitle}</span>
              </div>
              <h1 className="truncate text-lg font-semibold">{activeTitle}</h1>
            </div>
            <div className="flex items-center gap-2">
              <div className="hidden items-center gap-2 rounded-md border border-line/80 bg-surface/50 px-3 py-2 text-xs text-ink-muted md:flex">
                <Search className="h-3.5 w-3.5" />
                Search within each page
              </div>
              <button
                type="button"
                onClick={toggleTheme}
                className="flex h-9 w-9 items-center justify-center rounded-md border border-line/80 bg-surface/40 text-ink-muted hover:bg-surface-muted hover:text-ink"
                title="Toggle dark mode"
              >
                {dark ? <Sun className="h-4 w-4" /> : <Moon className="h-4 w-4" />}
              </button>
              <button
                type="button"
                onClick={logout}
                className="flex h-9 w-9 items-center justify-center rounded-md border border-line/80 bg-surface/40 text-ink-muted hover:bg-surface-muted hover:text-ink"
                title="Log out"
              >
                <LogOut className="h-4 w-4" />
              </button>
            </div>
          </div>
          <nav className="flex gap-1 overflow-x-auto border-t border-line/80 px-3 py-2 lg:hidden">
            {navItems.map((item) => {
              const Icon = item.icon;
              const active = pathname === item.href;
              return (
                <Link
                  key={item.href}
                  href={item.href}
                  className={clsx(
                    "flex shrink-0 items-center gap-2 rounded-md px-3 py-2 text-xs",
                    active ? "bg-accent/15 text-accent" : "text-ink-muted"
                  )}
                >
                  <Icon className="h-3.5 w-3.5" />
                  {item.label}
                </Link>
              );
            })}
          </nav>
        </header>
        <main className="mx-auto w-full max-w-7xl px-4 py-5 sm:px-6">
          <div className="mb-4 rounded-md border border-line/80 bg-surface-raised/75 px-4 py-3 text-sm text-ink-muted backdrop-blur">
            Signed in as{" "}
            <span className="font-medium text-ink">
              {user?.profile.displayName ?? "Companion user"}
            </span>
          </div>
          {children}
        </main>
      </div>
    </div>
  );
}
