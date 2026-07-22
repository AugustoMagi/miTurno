import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { listarPlanesPublicos } from '../api/planesPublicos'
import { extractError } from '../api/client'
import { Periodicidad } from '../types/plan'
import type { PlanPublico } from '../types/planPublico'
import { Spinner } from '../components/Spinner'
import { ErrorBanner } from '../components/ErrorBanner'
import { Reveal } from '../components/Reveal'

const PERIODICIDAD_LABEL: Record<Periodicidad, string> = {
  [Periodicidad.Mensual]: 'mes',
  [Periodicidad.Anual]: 'año',
}

// Los CTA de esta página son links (navegación), no acciones de formulario, así que se estilizan
// igual que <Button> pero sin usarlo: el componente no soporta renderizar como <Link>/<a>.
const BOTON = 'inline-flex items-center justify-center rounded-lg px-4 py-2.5 text-sm font-medium transition-colors'
const BOTON_PRIMARIO = `${BOTON} bg-emerald-600 text-white hover:bg-emerald-700`
const BOTON_SECUNDARIO = `${BOTON} bg-white text-slate-700 border border-slate-300 hover:bg-slate-100`

function IconShare() {
  return (
    <svg viewBox="0 0 24 24" fill="none" strokeWidth={1.8} stroke="currentColor" className="h-5 w-5">
      <path
        strokeLinecap="round"
        strokeLinejoin="round"
        d="M7.5 10.5 16.5 6M7.5 13.5 16.5 18M9 12a2.5 2.5 0 1 1-5 0 2.5 2.5 0 0 1 5 0Zm11-6a2.5 2.5 0 1 1-5 0 2.5 2.5 0 0 1 5 0Zm0 12a2.5 2.5 0 1 1-5 0 2.5 2.5 0 0 1 5 0Z"
      />
    </svg>
  )
}

function IconCalendarCheck() {
  return (
    <svg viewBox="0 0 24 24" fill="none" strokeWidth={1.8} stroke="currentColor" className="h-5 w-5">
      <path
        strokeLinecap="round"
        strokeLinejoin="round"
        d="M6.75 3v2.25M17.25 3v2.25M3.75 8.25h16.5M4.5 6h15a.75.75 0 0 1 .75.75V19.5a.75.75 0 0 1-.75.75h-15a.75.75 0 0 1-.75-.75V6.75A.75.75 0 0 1 4.5 6Zm4.19 8.19 1.81 1.81 3.81-3.81"
      />
    </svg>
  )
}

function IconCard() {
  return (
    <svg viewBox="0 0 24 24" fill="none" strokeWidth={1.8} stroke="currentColor" className="h-5 w-5">
      <path
        strokeLinecap="round"
        strokeLinejoin="round"
        d="M2.25 8.25h19.5M3.75 5.25h16.5a1.5 1.5 0 0 1 1.5 1.5v10.5a1.5 1.5 0 0 1-1.5 1.5H3.75a1.5 1.5 0 0 1-1.5-1.5V6.75a1.5 1.5 0 0 1 1.5-1.5ZM6 15h4.5"
      />
    </svg>
  )
}

function IconClock() {
  return (
    <svg viewBox="0 0 24 24" fill="none" strokeWidth={1.8} stroke="currentColor" className="h-5 w-5">
      <path strokeLinecap="round" strokeLinejoin="round" d="M12 7.5V12l3 1.5M21 12a9 9 0 1 1-18 0 9 9 0 0 1 18 0Z" />
    </svg>
  )
}

function IconChart() {
  return (
    <svg viewBox="0 0 24 24" fill="none" strokeWidth={1.8} stroke="currentColor" className="h-5 w-5">
      <path strokeLinecap="round" strokeLinejoin="round" d="M3 3v18h18M8 17V10m4.5 7V6m4.5 11v-4" />
    </svg>
  )
}

function IconGrid() {
  return (
    <svg viewBox="0 0 24 24" fill="none" strokeWidth={1.8} stroke="currentColor" className="h-5 w-5">
      <path
        strokeLinecap="round"
        strokeLinejoin="round"
        d="M4.5 4.5h6v6h-6v-6Zm9 0h6v6h-6v-6Zm-9 9h6v6h-6v-6Zm9 0h6v6h-6v-6Z"
      />
    </svg>
  )
}

const PASOS = [
  {
    titulo: 'Compartís tu link',
    texto: 'Lo pegás en la bio de Instagram, WhatsApp o donde ya te encuentran tus clientes.',
    icono: <IconShare />,
  },
  {
    titulo: 'Tu cliente reserva solo',
    texto: 'Elige cancha, día y horario disponible en segundos, sin escribirte para preguntar.',
    icono: <IconCalendarCheck />,
  },
  {
    titulo: 'Cobrás automático',
    texto: 'Paga con Mercado Pago o transferencia y la reserva queda confirmada sin que muevas un dedo.',
    icono: <IconCard />,
  },
]

const DEPORTES = [
  { nombre: 'Fútbol 5', emoji: '⚽', color: 'bg-emerald-50 text-emerald-700' },
  { nombre: 'Pádel', emoji: '🎾', color: 'bg-sky-50 text-sky-700' },
  { nombre: 'Tenis', emoji: '🎾', color: 'bg-amber-50 text-amber-700' },
  { nombre: 'Vóley', emoji: '🏐', color: 'bg-orange-50 text-orange-700' },
  { nombre: 'Básquet', emoji: '🏀', color: 'bg-rose-50 text-rose-700' },
  { nombre: 'Otras canchas', emoji: '🏟️', color: 'bg-violet-50 text-violet-700' },
]

const FAQS = [
  {
    pregunta: '¿Mis clientes necesitan instalar algo?',
    respuesta: 'No. Reservan desde el navegador, en el celular o la compu, sin descargar ninguna app.',
  },
  {
    pregunta: '¿Cómo cobro las reservas?',
    respuesta:
      'Podés conectar tu cuenta de Mercado Pago para cobrar online o dejar cargado tu alias para que te transfieran. Vos elegís cuál usar.',
  },
  {
    pregunta: '¿Puedo probarlo sin pagar?',
    respuesta: 'Sí, arrancás con el plan de prueba gratis y sin cargar ninguna tarjeta.',
  },
  {
    pregunta: '¿Qué pasa si necesito bloquear un horario o cancelar una reserva?',
    respuesta:
      'Desde tu panel podés bloquear horarios puntuales (mantenimiento, torneo, etc.) y confirmar o rechazar pagos cuando haga falta.',
  },
]

const BENEFICIOS = [
  {
    titulo: 'Disponibilidad real',
    texto: 'Los horarios ya reservados desaparecen al instante, cero dobles reservas.',
    icono: <IconClock />,
  },
  {
    titulo: 'Pagos integrados',
    texto: 'Mercado Pago conectado o tu alias para transferencias manuales, vos elegís.',
    icono: <IconCard />,
  },
  {
    titulo: 'Estadísticas de ocupación',
    texto: 'Mirá qué días y horarios se llenan primero para ajustar precios y turnos.',
    icono: <IconChart />,
  },
  {
    titulo: 'Todo en un panel',
    texto: 'Canchas, horarios, clientes y reservas, sin planillas ni cuadernos.',
    icono: <IconGrid />,
  },
]

function PlanCard({ plan }: { plan: PlanPublico }) {
  return (
    <div
      className={`flex h-full flex-col gap-4 rounded-2xl border bg-white p-6 shadow-sm transition-all duration-300 hover:-translate-y-1 hover:shadow-lg ${
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
      <header className="sticky top-0 z-20 border-b border-slate-200/80 bg-white/70 backdrop-blur-md">
        <div className="mx-auto flex max-w-6xl items-center justify-between px-4 py-3.5">
          <Link to="/" className="group flex items-center gap-2">
            <span className="flex h-9 w-9 items-center justify-center rounded-xl bg-gradient-to-br from-emerald-500 to-teal-500 text-white shadow-md shadow-emerald-600/30 transition-transform duration-300 group-hover:scale-105 group-hover:rotate-6">
              <IconCalendarCheck />
            </span>
            <span className="text-lg font-semibold text-slate-900">
              Mi
              <span className="bg-gradient-to-r from-emerald-600 to-teal-500 bg-clip-text text-transparent">
                Turno
              </span>
            </span>
          </Link>
          <nav className="flex items-center gap-2 sm:gap-6">
            <a
              href="#planes"
              className="group hidden text-sm font-medium text-slate-600 hover:text-slate-900 sm:inline"
            >
              Planes
              <span className="block h-0.5 max-w-0 bg-gradient-to-r from-emerald-500 to-teal-500 transition-all duration-300 group-hover:max-w-full" />
            </a>
            <Link
              to="/panel/login"
              className="group text-sm font-medium text-slate-600 hover:text-slate-900"
            >
              Ingresar
              <span className="block h-0.5 max-w-0 bg-gradient-to-r from-emerald-500 to-teal-500 transition-all duration-300 group-hover:max-w-full" />
            </Link>
            <Link
              to="/panel/registro"
              className={`${BOTON_PRIMARIO} hidden shadow-sm shadow-emerald-600/20 hover:-translate-y-0.5 hover:shadow-md hover:shadow-emerald-600/30 sm:inline-flex`}
            >
              Crear tu negocio
            </Link>
          </nav>
        </div>
      </header>

      <main className="flex-1">
        <section className="relative overflow-hidden">
          <div className="pointer-events-none absolute inset-0 -z-10 overflow-hidden">
            <div className="animate-blob absolute -top-32 -right-32 h-96 w-96 rounded-full bg-emerald-200/70 blur-3xl" />
            <div className="animate-blob-delay absolute top-40 -left-32 h-72 w-72 rounded-full bg-teal-100 blur-3xl" />
            <div className="animate-blob absolute bottom-0 left-1/3 h-64 w-64 rounded-full bg-emerald-50 blur-3xl" />
          </div>

          <div className="mx-auto grid max-w-6xl gap-12 px-4 py-16 sm:py-24 lg:grid-cols-2 lg:items-center">
            <div className="animate-fade-in-up flex flex-col gap-6">
              <span className="inline-flex w-fit items-center gap-1.5 rounded-full bg-emerald-50 px-3 py-1 text-xs font-semibold text-emerald-700 ring-1 ring-emerald-200">
                <span className="h-1.5 w-1.5 animate-pulse rounded-full bg-emerald-500" />
                Reservas online para tu cancha
              </span>
              <h1 className="text-4xl font-bold tracking-tight text-slate-900 sm:text-5xl">
                Reservas de cancha,{' '}
                <span className="relative inline-block">
                  <span className="bg-gradient-to-r from-emerald-600 to-teal-500 bg-clip-text text-transparent">
                    sin llamados ni WhatsApp perdido.
                  </span>
                  <svg
                    viewBox="0 0 300 12"
                    preserveAspectRatio="none"
                    className="absolute -bottom-1 left-0 h-2.5 w-full text-emerald-300"
                  >
                    <path
                      d="M2 9c40-6 220-6 296 0"
                      fill="none"
                      stroke="currentColor"
                      strokeWidth="4"
                      strokeLinecap="round"
                    />
                  </svg>
                </span>
              </h1>
              <p className="text-lg text-slate-600">
                Compartí un link, tus clientes eligen día y horario disponible, pagan y quedan
                confirmados al instante. Vos mirás el panel y listo.
              </p>
              <div className="flex flex-wrap gap-3">
                <Link
                  to="/panel/registro"
                  className={`${BOTON_PRIMARIO} px-6 py-3 text-base shadow-md shadow-emerald-600/20 hover:-translate-y-0.5 hover:shadow-lg hover:shadow-emerald-600/30`}
                >
                  Crear tu negocio gratis
                </Link>
                <a
                  href="#planes"
                  className={`${BOTON_SECUNDARIO} px-6 py-3 text-base hover:-translate-y-0.5`}
                >
                  Ver planes
                </a>
              </div>
            </div>

            <div className="flex justify-center lg:justify-end">
              <div className="animate-card-float w-full max-w-sm rounded-2xl border border-slate-200 bg-white p-5 shadow-xl transition-shadow duration-300 hover:shadow-2xl">
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

        <section id="como-funciona" className="relative overflow-hidden px-4 py-16">
          <div className="pointer-events-none absolute inset-0 -z-10 overflow-hidden">
            <div className="animate-blob absolute -top-20 -left-24 h-72 w-72 rounded-full bg-teal-100/70 blur-3xl" />
            <div className="animate-blob-delay absolute -bottom-24 -right-16 h-72 w-72 rounded-full bg-emerald-100/70 blur-3xl" />
          </div>
          <div className="mx-auto max-w-6xl">
          <Reveal>
            <div className="flex flex-col items-center gap-3">
              <span className="rounded-full bg-emerald-50 px-3 py-1 text-xs font-semibold tracking-wide text-emerald-700 uppercase">
                3 pasos simples
              </span>
              <h2 className="text-center text-2xl font-semibold text-slate-900 sm:text-3xl">
                Cómo{' '}
                <span className="bg-gradient-to-r from-emerald-600 to-teal-500 bg-clip-text text-transparent">
                  funciona
                </span>
              </h2>
            </div>
          </Reveal>
          <div className="mt-10 grid gap-8 sm:grid-cols-3">
            {PASOS.map((paso, i) => (
              <Reveal key={paso.titulo} delayMs={i * 100}>
                <div className="group relative flex h-full flex-col items-start gap-3 overflow-hidden rounded-2xl border border-slate-200 bg-white p-6 shadow-sm transition-all duration-300 hover:-translate-y-1.5 hover:border-emerald-200 hover:shadow-xl">
                  <div className="absolute inset-x-0 top-0 h-1.5 bg-gradient-to-r from-emerald-500 to-teal-400" />
                  <div className="flex items-center gap-3">
                    <span className="flex h-11 w-11 items-center justify-center rounded-full bg-gradient-to-br from-emerald-500 to-teal-500 text-sm font-semibold text-white shadow-md shadow-emerald-600/30 transition-transform duration-300 group-hover:scale-110">
                      {i + 1}
                    </span>
                    <span className="flex h-9 w-9 items-center justify-center rounded-lg bg-emerald-50 text-emerald-600">
                      {paso.icono}
                    </span>
                  </div>
                  <h3 className="font-semibold text-slate-900">{paso.titulo}</h3>
                  <p className="text-sm text-slate-600">{paso.texto}</p>
                </div>
              </Reveal>
            ))}
          </div>
          </div>
        </section>

        <section className="relative overflow-hidden px-4 py-16">
          <div className="pointer-events-none absolute inset-0 -z-10 overflow-hidden">
            <div className="animate-blob absolute top-0 right-0 h-72 w-72 rounded-full bg-sky-100/60 blur-3xl" />
            <div className="animate-blob-delay absolute bottom-0 left-1/4 h-64 w-64 rounded-full bg-emerald-100/60 blur-3xl" />
          </div>
          <div className="mx-auto max-w-6xl">
          <Reveal>
            <h2 className="text-center text-2xl font-semibold text-slate-900 sm:text-3xl">
              Sirve para cualquier tipo de cancha
            </h2>
            <p className="mx-auto mt-2 max-w-xl text-center text-slate-600">
              Configurás tus recursos y horarios una vez, y funciona igual sea cual sea el deporte.
            </p>
          </Reveal>
          <div className="mt-10 flex flex-wrap justify-center gap-4">
            {DEPORTES.map((d, i) => (
              <Reveal key={d.nombre} delayMs={i * 60}>
                <div
                  className={`flex items-center gap-2 rounded-full px-5 py-3 text-sm font-medium shadow-sm transition-transform duration-300 hover:-translate-y-1 hover:shadow-md ${d.color}`}
                >
                  <span className="text-lg">{d.emoji}</span>
                  {d.nombre}
                </div>
              </Reveal>
            ))}
          </div>
          </div>
        </section>

        <section className="bg-slate-50 py-16">
          <div className="mx-auto max-w-6xl px-4">
            <Reveal>
              <h2 className="text-center text-2xl font-semibold text-slate-900 sm:text-3xl">
                Todo lo que necesitás para dejar de anotar turnos a mano
              </h2>
            </Reveal>
            <div className="mt-10 grid gap-6 sm:grid-cols-2 lg:grid-cols-4">
              {BENEFICIOS.map((b, i) => (
                <Reveal key={b.titulo} delayMs={i * 80}>
                  <div className="group h-full rounded-xl border border-slate-200 bg-white p-5 transition-all duration-300 hover:-translate-y-1 hover:border-emerald-200 hover:shadow-lg">
                    <span className="flex h-9 w-9 items-center justify-center rounded-lg bg-emerald-50 text-emerald-600 transition-colors duration-300 group-hover:bg-emerald-600 group-hover:text-white">
                      {b.icono}
                    </span>
                    <h3 className="mt-3 font-semibold text-slate-900">{b.titulo}</h3>
                    <p className="mt-2 text-sm text-slate-600">{b.texto}</p>
                  </div>
                </Reveal>
              ))}
            </div>
          </div>
        </section>

        <section id="planes" className="relative overflow-hidden px-4 py-16">
          <div className="pointer-events-none absolute inset-0 -z-10 overflow-hidden">
            <div className="animate-blob absolute -top-16 left-1/4 h-72 w-72 rounded-full bg-emerald-100/60 blur-3xl" />
            <div className="animate-blob-delay absolute -bottom-20 right-1/4 h-72 w-72 rounded-full bg-teal-100/60 blur-3xl" />
          </div>
          <div className="mx-auto max-w-6xl">
          <Reveal>
            <h2 className="text-center text-2xl font-semibold text-slate-900 sm:text-3xl">Planes</h2>
            <p className="mx-auto mt-2 max-w-xl text-center text-slate-600">
              Elegí el plan según cuántas canchas manejás. Podés cambiarlo cuando quieras.
            </p>
          </Reveal>

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
                {planes.map((plan, i) => (
                  <Reveal key={plan.id} delayMs={i * 80}>
                    <PlanCard plan={plan} />
                  </Reveal>
                ))}
              </div>
            )}
          </div>
          </div>
        </section>

        <section className="bg-slate-50 py-16">
          <div className="mx-auto max-w-3xl px-4">
            <Reveal>
              <h2 className="text-center text-2xl font-semibold text-slate-900 sm:text-3xl">
                Preguntas frecuentes
              </h2>
            </Reveal>
            <div className="mt-10 flex flex-col gap-3">
              {FAQS.map((f, i) => (
                <Reveal key={f.pregunta} delayMs={i * 60}>
                  <details className="group rounded-xl border border-slate-200 bg-white p-5 open:border-emerald-200 open:shadow-sm">
                    <summary className="flex cursor-pointer list-none items-center justify-between gap-4 font-medium text-slate-900">
                      {f.pregunta}
                      <span className="shrink-0 text-emerald-600 transition-transform duration-300 group-open:rotate-45">
                        <svg viewBox="0 0 24 24" fill="none" strokeWidth={2} stroke="currentColor" className="h-5 w-5">
                          <path strokeLinecap="round" strokeLinejoin="round" d="M12 4.5v15m7.5-7.5h-15" />
                        </svg>
                      </span>
                    </summary>
                    <p className="mt-3 text-sm text-slate-600">{f.respuesta}</p>
                  </details>
                </Reveal>
              ))}
            </div>
          </div>
        </section>

        <section
          className="relative overflow-hidden bg-cover bg-center"
          style={{
            backgroundImage:
              'linear-gradient(to right, rgba(5,150,105,0.93), rgba(13,148,136,0.93)), url(/images/cancha-hero.jpg)',
          }}
        >
          <div className="pointer-events-none absolute inset-0 -z-10 overflow-hidden">
            <div className="animate-blob absolute -top-24 right-10 h-72 w-72 rounded-full bg-white/10 blur-3xl" />
            <div className="animate-blob-delay absolute -bottom-24 left-10 h-72 w-72 rounded-full bg-white/10 blur-3xl" />
          </div>
          <Reveal>
            <div className="mx-auto flex max-w-6xl flex-col items-center gap-4 px-4 py-16 text-center">
              <h2 className="text-2xl font-semibold text-white sm:text-3xl">
                ¿Tenés una cancha y querés recibir reservas online?
              </h2>
              <p className="max-w-xl text-emerald-50">
                Creá tu cuenta en un minuto y compartí tu link hoy mismo.
              </p>
              <Link
                to="/panel/registro"
                className={`${BOTON} bg-white px-6 py-3 text-base text-emerald-700 shadow-lg hover:-translate-y-0.5 hover:bg-emerald-50`}
              >
                Crear tu negocio gratis
              </Link>
            </div>
          </Reveal>
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
            <p className="font-medium text-slate-700">¿Necesitás ayuda? Escribinos</p>
            <a href="tel:+543412853608" className="text-slate-600 hover:text-slate-900">
              Tel / WhatsApp: 341 285-3608
            </a>
            <Link to="/admin/login" className="text-slate-400 hover:text-slate-600">
              Acceso administrador
            </Link>
          </div>
        </div>
      </footer>
    </div>
  )
}
