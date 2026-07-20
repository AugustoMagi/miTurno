import { useState } from 'react'
import { NavLink, Outlet, useNavigate } from 'react-router-dom'
import { useAdminAuth } from '../context/AdminAuthContext'

const NAV_ITEMS = [
  { to: '/admin/planes', label: 'Planes' },
  { to: '/admin/suscripciones', label: 'Suscripciones' },
]

function NavLinks({ onNavigate }: { onNavigate?: () => void }) {
  return (
    <>
      {NAV_ITEMS.map((item) => (
        <NavLink
          key={item.to}
          to={item.to}
          onClick={onNavigate}
          className={({ isActive }) =>
            `rounded-lg px-3 py-2 text-sm font-medium transition-colors ${
              isActive ? 'bg-emerald-50 text-emerald-700' : 'text-slate-600 hover:bg-slate-100'
            }`
          }
        >
          {item.label}
        </NavLink>
      ))}
    </>
  )
}

export function AdminLayout() {
  const { sesion, logout } = useAdminAuth()
  const navigate = useNavigate()
  const [menuAbierto, setMenuAbierto] = useState(false)

  function handleLogout() {
    logout()
    navigate('/admin/login')
  }

  return (
    <div className="flex min-h-svh flex-col lg:flex-row">
      <header className="flex items-center justify-between border-b border-slate-200 bg-white px-4 py-3 lg:hidden">
        <span className="text-lg font-semibold text-slate-900">
          Mi<span className="text-emerald-600">Turno</span> <span className="text-slate-400">Admin</span>
        </span>
        <button
          type="button"
          onClick={() => setMenuAbierto((open) => !open)}
          className="rounded-lg border border-slate-300 px-3 py-1.5 text-sm text-slate-700"
        >
          Menú
        </button>
      </header>

      {menuAbierto && (
        <nav className="flex flex-col gap-1 border-b border-slate-200 bg-white px-3 py-3 lg:hidden">
          <NavLinks onNavigate={() => setMenuAbierto(false)} />
          <button
            type="button"
            onClick={handleLogout}
            className="mt-1 rounded-lg px-3 py-2 text-left text-sm font-medium text-red-600 hover:bg-red-50"
          >
            Salir
          </button>
        </nav>
      )}

      <aside className="hidden w-60 shrink-0 flex-col border-r border-slate-200 bg-white lg:flex">
        <div className="px-5 py-5">
          <span className="text-lg font-semibold text-slate-900">
            Mi<span className="text-emerald-600">Turno</span>
          </span>
          <p className="mt-0.5 text-xs font-medium uppercase tracking-wide text-slate-400">Admin</p>
          {sesion && <p className="mt-1 truncate text-xs text-slate-400">{sesion.nombre}</p>}
        </div>
        <nav className="flex flex-1 flex-col gap-1 px-3">
          <NavLinks />
        </nav>
        <div className="px-3 py-4">
          <button
            type="button"
            onClick={handleLogout}
            className="w-full rounded-lg px-3 py-2 text-left text-sm font-medium text-red-600 hover:bg-red-50"
          >
            Salir
          </button>
        </div>
      </aside>

      <main className="flex-1 bg-slate-50 px-4 py-6 sm:px-6 lg:px-8 lg:py-8">
        <div className="mx-auto max-w-5xl">
          <Outlet />
        </div>
      </main>
    </div>
  )
}
