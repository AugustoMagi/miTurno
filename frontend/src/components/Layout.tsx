import type { ReactNode } from 'react'
import { Link } from 'react-router-dom'

export function Layout({ children }: { children: ReactNode }) {
  return (
    <div className="bg-dotted flex min-h-svh flex-col">
      <header className="border-b border-slate-200 bg-white/80 backdrop-blur-sm">
        <div className="mx-auto flex max-w-3xl items-center px-4 py-4 sm:max-w-5xl sm:px-6">
          <Link to="/" className="flex items-center gap-2 transition-opacity hover:opacity-80">
            <img src="/logo.png" alt="MiTurno" className="h-8 w-8 rounded-lg object-cover shadow-soft" />
            <span
              className="text-lg font-bold tracking-tight text-slate-900"
              style={{ fontFamily: 'var(--font-heading)' }}
            >
              Mi<span className="text-accent-500">Turno</span>
            </span>
          </Link>
        </div>
      </header>
      <main className="mx-auto w-full max-w-3xl flex-1 px-4 py-8 sm:max-w-5xl sm:px-6">{children}</main>
      <footer className="border-t border-slate-200 py-6 text-center text-xs text-slate-400">
        MiTurno · Reservá tu turno en segundos
      </footer>
    </div>
  )
}
