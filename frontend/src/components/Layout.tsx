import type { ReactNode } from 'react'
import { Link } from 'react-router-dom'

export function Layout({ children }: { children: ReactNode }) {
  return (
    <div className="flex min-h-svh flex-col">
      <header className="border-b border-slate-200 bg-white">
        <div className="mx-auto flex max-w-3xl items-center px-4 py-4 sm:max-w-5xl">
          <Link to="/" className="text-lg font-semibold text-slate-900">
            Mi<span className="text-emerald-600">Turno</span>
          </Link>
        </div>
      </header>
      <main className="mx-auto w-full max-w-3xl flex-1 px-4 py-8 sm:max-w-5xl">{children}</main>
      <footer className="border-t border-slate-200 py-6 text-center text-xs text-slate-400">
        MiTurno · Reservá tu turno en segundos
      </footer>
    </div>
  )
}
