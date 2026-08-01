import { useState } from 'react'
import type { ReactNode } from 'react'
import { NavLink, Outlet, useNavigate } from 'react-router-dom'
import { useAuth } from '../context/AuthContext'
import {
  BuildingIcon,
  CalendarIcon,
  ChartIcon,
  CreditCardIcon,
  GridIcon,
  LogOutIcon,
  MenuIcon,
  StarIcon,
  UserIcon,
  UsersIcon,
  XIcon,
} from './icons'

const NAV_ITEMS: { to: string; label: string; icon: (props: { className?: string }) => ReactNode }[] = [
  { to: '/panel/estadisticas', label: 'Estadísticas', icon: ChartIcon },
  { to: '/panel/recursos', label: 'Recursos', icon: GridIcon },
  { to: '/panel/reservas', label: 'Reservas', icon: CalendarIcon },
  { to: '/panel/clientes', label: 'Clientes', icon: UsersIcon },
  { to: '/panel/configuracion-pago', label: 'Cobro', icon: CreditCardIcon },
  { to: '/panel/suscripcion', label: 'Suscripción', icon: StarIcon },
  { to: '/panel/negocio', label: 'Mi negocio', icon: BuildingIcon },
  { to: '/panel/perfil', label: 'Perfil', icon: UserIcon },
]

function NavLinks({ onNavigate }: { onNavigate?: () => void }) {
  return (
    <>
      {NAV_ITEMS.map((item) => {
        const Icon = item.icon
        return (
          <NavLink
            key={item.to}
            to={item.to}
            onClick={onNavigate}
            className={({ isActive }) =>
              `flex items-center gap-2.5 rounded-xl px-3 py-2.5 text-sm font-medium transition-all duration-200 ${
                isActive
                  ? 'bg-link-50 text-link-700'
                  : 'text-slate-600 hover:bg-slate-100 hover:text-slate-900'
              }`
            }
          >
            <Icon className="h-4.5 w-4.5 shrink-0" />
            {item.label}
          </NavLink>
        )
      })}
    </>
  )
}

export function PanelLayout() {
  const { sesion, logout } = useAuth()
  const navigate = useNavigate()
  const [menuAbierto, setMenuAbierto] = useState(false)

  function handleLogout() {
    logout()
    navigate('/panel/login')
  }

  return (
    <div className="flex min-h-svh flex-col lg:flex-row">
      <header className="flex items-center justify-between border-b border-slate-200 bg-white px-4 py-3 lg:hidden">
        <span className="flex items-center gap-2">
          <img src="/logo.png" alt="MiTurno" className="h-8 w-8 rounded-lg object-cover shadow-soft" />
          <span className="text-lg font-bold tracking-tight text-slate-900" style={{ fontFamily: 'var(--font-heading)' }}>
            Mi<span className="text-accent-500">Turno</span>
          </span>
        </span>
        <button
          type="button"
          onClick={() => setMenuAbierto((open) => !open)}
          aria-label={menuAbierto ? 'Cerrar menú' : 'Abrir menú'}
          className="flex h-9 w-9 items-center justify-center rounded-xl border border-slate-300 text-slate-700 transition-colors duration-200 hover:bg-slate-100"
        >
          {menuAbierto ? <XIcon className="h-5 w-5" /> : <MenuIcon className="h-5 w-5" />}
        </button>
      </header>

      {menuAbierto && (
        <nav className="animate-fade-in-up flex flex-col gap-1 border-b border-slate-200 bg-white px-3 py-3 lg:hidden">
          <NavLinks onNavigate={() => setMenuAbierto(false)} />
          <button
            type="button"
            onClick={handleLogout}
            className="mt-1 flex items-center gap-2.5 rounded-xl px-3 py-2.5 text-left text-sm font-medium text-red-600 transition-colors duration-200 hover:bg-red-50"
          >
            <LogOutIcon className="h-4.5 w-4.5" />
            Salir
          </button>
        </nav>
      )}

      <aside className="hidden w-64 shrink-0 flex-col border-r border-slate-200 bg-white lg:flex">
        <div className="px-5 py-6">
          <span className="flex items-center gap-2">
            <img src="/logo.png" alt="MiTurno" className="h-8 w-8 rounded-lg object-cover shadow-soft" />
            <span className="text-lg font-bold tracking-tight text-slate-900" style={{ fontFamily: 'var(--font-heading)' }}>
              Mi<span className="text-accent-500">Turno</span>
            </span>
          </span>
          {sesion && <p className="mt-1.5 truncate text-xs text-slate-400">{sesion.nombre}</p>}
        </div>
        <nav className="flex flex-1 flex-col gap-1 px-3">
          <NavLinks />
        </nav>
        <div className="px-3 py-4">
          <button
            type="button"
            onClick={handleLogout}
            className="flex w-full items-center gap-2.5 rounded-xl px-3 py-2.5 text-left text-sm font-medium text-red-600 transition-colors duration-200 hover:bg-red-50"
          >
            <LogOutIcon className="h-4.5 w-4.5" />
            Salir
          </button>
        </div>
      </aside>

      <main className="bg-dotted flex-1 px-4 py-6 sm:px-6 lg:px-8 lg:py-8">
        <div className="mx-auto max-w-5xl">
          <Outlet />
        </div>
      </main>
    </div>
  )
}
