import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { listarPlanesPublicos } from '../api/planesPublicos'
import { extractError } from '../api/client'
import { Periodicidad } from '../types/plan'
import type { PlanPublico } from '../types/planPublico'
import { Spinner } from '../components/Spinner'
import { ErrorBanner } from '../components/ErrorBanner'

const PERIODICIDAD_LABEL: Record<Periodicidad, string> = {
  [Periodicidad.Mensual]: 'mes',
  [Periodicidad.Anual]: 'año',
}

// Los CTA de esta página son links (navegación), no acciones de formulario, así que se estilizan
// igual que <Button> pero sin usarlo: el componente no soporta renderizar como <Link>/<a>.
const BOTON = 'inline-flex items-center justify-center rounded-lg px-4 py-2.5 text-sm font-medium transition-colors'
const BOTON_PRIMARIO = `${BOTON} bg-emerald-600 text-white hover:bg-emerald-700`
const BOTON_SECUNDARIO = `${BOTON} bg-white text-slate-700 border border-slate-300 hover:bg-slate-100`

const PASOS = [
  {
    titulo: 'Compartís tu link',
    texto: 'Lo pegás en la bio de Instagram, WhatsApp o donde ya te encuentran tus clientes.',
  },
  {
    titulo: 'Tu cliente reserva solo',
    texto: 'Elige cancha, día y horario disponible en segundos, sin escribirte para preguntar.',
  },
  {
    titulo: 'Cobrás automático',
    texto: 'Paga con Mercado Pago o transferencia y la reserva queda confirmada sin que muevas un dedo.',
  },
]

const BENEFICIOS = [
  {
    titulo: 'Disponibilidad real',
    texto: 'Los horarios ya reservados desaparecen al instante, cero dobles reservas.',
  },
  {
    titulo: 'Pagos integrados',
    texto: 'Mercado Pago conectado o tu alias para transferencias manuales, vos elegís.',
  },
  {
    titulo: 'Estadísticas de ocupación',
    texto: 'Mirá qué días y horarios se llenan primero para ajustar precios y turnos.',
  },
  {
    titulo: 'Todo en un panel',
    texto: 'Canchas, horarios, clientes y reservas, sin planillas ni cuadernos.',
  },
]

function PlanCard({ plan }: { plan: PlanPublico }) {
  return (
    <div
      className={`flex flex-col gap-4 rounded-2xl border bg-white p-6 shadow-sm ${
        plan.esPlanDePrueba ? 'border-emerald-300 ring-1 ring-emerald-200' : 'border-slate-200'
      }`}
    >
      <div className="flex items-center gap-2">
        <h3 className="text-lg font-semibold text-slate-900">{plan.nombre}</h3>
        {plan.esPlanDePrueba && (
          <span className="rounded-full bg-emerald-50 px-2 py-0.5 text-xs font-medium text-emerald-700">
            Para empezar
          </span>
        )}
      </div>
      <p className="text-slate-900">
        <span className="text-3xl font-bold">${plan.precio.toLocaleString('es-AR')}</span>
        <span className="text-sm text-slate-500"> / {PERIODICIDAD_LABEL[plan.periodicidad]}</span>
      </p>
      <ul className="flex flex-1 flex-col gap-2 text-sm text-slate-600">
        <li>Hasta {plan.limiteRecursos} cancha{plan.limiteRecursos === 1 ? '' : 's'}</li>
        <li>{plan.limiteReservasPorMes} reservas por mes</li>
      </ul>
      <Link to="/panel/registro" className={`${BOTON_PRIMARIO} mt-auto`}>
        Empezar
      </Link>
    </div>
  )
}

export function LandingPage() {
  const [planes, setPlanes] = useState<PlanPublico[] | null>(null)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    listarPlanesPublicos()
      .then(setPlanes)
      .catch((err) => setError(extractError(err)))
  }, [])

  return (
    <div className="flex min-h-svh flex-col bg-white">
      <header className="sticky top-0 z-10 border-b border-slate-200 bg-white/80 backdrop-blur">
        <div className="mx-auto flex max-w-6xl items-center justify-between px-4 py-4">
          <Link to="/" className="text-lg font-semibold text-slate-900">
            Mi<span className="text-emerald-600">Turno</span>
          </Link>
          <nav className="flex items-center gap-2 sm:gap-4">
            <a
              href="#planes"
              className="hidden text-sm font-medium text-slate-600 hover:text-slate-900 sm:inline"
            >
              Planes
            </a>
            <Link
              to="/panel/login"
              className="text-sm font-medium text-slate-600 hover:text-slate-900"
            >
              Ingresar
            </Link>
            <Link to="/panel/registro" className={`${BOTON_PRIMARIO} hidden sm:inline-flex`}>
              Crear tu negocio
            </Link>
          </nav>
        </div>
      </header>

      <main className="flex-1">
        <section className="relative overflow-hidden">
          <div className="pointer-events-none absolute inset-0 -z-10 overflow-hidden">
            <div className="absolute -top-32 -right-32 h-96 w-96 rounded-full bg-emerald-100 blur-3xl" />
            <div className="absolute top-40 -left-32 h-72 w-72 rounded-full bg-emerald-50 blur-3xl" />
          </div>

          <div className="mx-auto grid max-w-6xl gap-12 px-4 py-16 sm:py-24 lg:grid-cols-2 lg:items-center">
            <div className="flex flex-col gap-6">
              <h1 className="text-4xl font-bold tracking-tight text-slate-900 sm:text-5xl">
                Reservas de cancha, sin llamados ni WhatsApp perdido.
              </h1>
              <p className="text-lg text-slate-600">
                Compartí un link, tus clientes eligen día y horario disponible, pagan y quedan
                confirmados al instante. Vos mirás el panel y listo.
              </p>
              <div className="flex flex-wrap gap-3">
                <Link to="/panel/registro" className={`${BOTON_PRIMARIO} px-6 py-3 text-base`}>
                  Crear tu negocio gratis
                </Link>
                <a href="#planes" className={`${BOTON_SECUNDARIO} px-6 py-3 text-base`}>
                  Ver planes
                </a>
              </div>
            </div>

            <div className="flex justify-center lg:justify-end">
              <div className="w-full max-w-sm rotate-2 rounded-2xl border border-slate-200 bg-white p-5 shadow-xl">
                <p className="text-sm font-medium text-slate-500">Cancha Norte · Fútbol 5</p>
                <p className="mt-1 text-lg font-semibold text-slate-900">Hoy, 20:00 - 21:00</p>
                <div className="mt-4 grid grid-cols-3 gap-2 text-center text-sm">
                  <span className="rounded-lg border border-slate-200 py-2 text-slate-400 line-through">
                    18:00
                  </span>
                  <span className="rounded-lg border border-slate-200 py-2 text-slate-400 line-through">
                    19:00
                  </span>
                  <span className="rounded-lg border-2 border-emerald-600 bg-emerald-50 py-2 font-semibold text-emerald-700">
                    20:00
                  </span>
                </div>
                <div className="mt-4 flex items-center justify-between border-t border-slate-100 pt-4">
                  <span className="text-sm text-slate-500">Total</span>
                  <span className="font-semibold text-slate-900">$5.000</span>
                </div>
              </div>
            </div>
          </div>
        </section>

        <section id="como-funciona" className="mx-auto max-w-6xl px-4 py-16">
          <h2 className="text-center text-2xl font-semibold text-slate-900 sm:text-3xl">
            Cómo funciona
          </h2>
          <div className="mt-10 grid gap-8 sm:grid-cols-3">
            {PASOS.map((paso, i) => (
              <div key={paso.titulo} className="flex flex-col items-start gap-3">
                <span className="flex h-10 w-10 items-center justify-center rounded-full bg-emerald-600 text-sm font-semibold text-white">
                  {i + 1}
                </span>
                <h3 className="font-semibold text-slate-900">{paso.titulo}</h3>
                <p className="text-sm text-slate-600">{paso.texto}</p>
              </div>
            ))}
          </div>
        </section>

        <section className="bg-slate-50 py-16">
          <div className="mx-auto max-w-6xl px-4">
            <h2 className="text-center text-2xl font-semibold text-slate-900 sm:text-3xl">
              Todo lo que necesitás para dejar de anotar turnos a mano
            </h2>
            <div className="mt-10 grid gap-6 sm:grid-cols-2 lg:grid-cols-4">
              {BENEFICIOS.map((b) => (
                <div key={b.titulo} className="rounded-xl border border-slate-200 bg-white p-5">
                  <h3 className="font-semibold text-slate-900">{b.titulo}</h3>
                  <p className="mt-2 text-sm text-slate-600">{b.texto}</p>
                </div>
              ))}
            </div>
          </div>
        </section>

        <section id="planes" className="mx-auto max-w-6xl px-4 py-16">
          <h2 className="text-center text-2xl font-semibold text-slate-900 sm:text-3xl">Planes</h2>
          <p className="mx-auto mt-2 max-w-xl text-center text-slate-600">
            Elegí el plan según cuántas canchas manejás. Podés cambiarlo cuando quieras.
          </p>

          <div className="mt-10">
            {error && <ErrorBanner message={error} />}
            {!error && !planes && <Spinner label="Cargando planes…" />}
            {!error && planes && planes.length === 0 && (
              <p className="text-center text-slate-500">
                Todavía no hay planes publicados. Creá tu negocio y arrancás con la prueba gratis.
              </p>
            )}
            {!error && planes && planes.length > 0 && (
              <div className="grid gap-6 sm:grid-cols-2 lg:grid-cols-3">
                {planes.map((plan) => (
                  <PlanCard key={plan.id} plan={plan} />
                ))}
              </div>
            )}
          </div>
        </section>

        <section className="bg-emerald-600">
          <div className="mx-auto flex max-w-6xl flex-col items-center gap-4 px-4 py-16 text-center">
            <h2 className="text-2xl font-semibold text-white sm:text-3xl">
              ¿Tenés una cancha y querés recibir reservas online?
            </h2>
            <p className="max-w-xl text-emerald-50">
              Creá tu cuenta en un minuto y compartí tu link hoy mismo.
            </p>
            <Link
              to="/panel/registro"
              className={`${BOTON} bg-white px-6 py-3 text-base text-emerald-700 hover:bg-emerald-50`}
            >
              Crear tu negocio gratis
            </Link>
          </div>
        </section>
      </main>

      <footer className="border-t border-slate-200 py-10">
        <div className="mx-auto flex max-w-6xl flex-col items-center gap-6 px-4 sm:flex-row sm:items-start sm:justify-between">
          <div>
            <p className="text-lg font-semibold text-slate-900">
              Mi<span className="text-emerald-600">Turno</span>
            </p>
            <p className="mt-1 text-sm text-slate-500">Reservá tu turno en segundos.</p>
          </div>
          <div className="flex flex-col gap-1 text-sm sm:items-end">
            <div className="flex gap-4">
              <Link to="/panel/login" className="text-slate-600 hover:text-slate-900">
                Ingresar (dueño)
              </Link>
              <Link to="/panel/registro" className="text-slate-600 hover:text-slate-900">
                Crear cuenta
              </Link>
            </div>
            <Link to="/admin/login" className="text-slate-400 hover:text-slate-600">
              Acceso administrador
            </Link>
          </div>
        </div>
      </footer>
    </div>
  )
}
