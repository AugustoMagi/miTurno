import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { listarPlanesPublicos } from '../api/planesPublicos'
import { extractError } from '../api/client'
import { Periodicidad } from '../types/plan'
import type { PlanPublico } from '../types/planPublico'
import { Spinner } from '../components/Spinner'
import { ErrorBanner } from '../components/ErrorBanner'
import { Reveal } from '../components/Reveal'
import { HeroCanchasFondo } from '../components/HeroCanchasFondo'

const PERIODICIDAD_LABEL: Record<Periodicidad, string> = {
  [Periodicidad.Mensual]: 'mes',
  [Periodicidad.Anual]: 'año',
}

// Los CTA de esta página son links (navegación), no acciones de formulario, así que se estilizan
// igual que <Button> pero sin usarlo: el componente no soporta renderizar como <Link>/<a>.
const BOTON =
  'inline-flex h-11 items-center justify-center gap-2 rounded-xl px-5 text-sm font-medium transition-all duration-200'
const BOTON_PRIMARIO = `${BOTON} bg-accent-500 text-white shadow-soft hover:bg-accent-600 hover:shadow-soft-lg hover:-translate-y-0.5 active:bg-accent-700`
const BOTON_SECUNDARIO = `${BOTON} border-2 border-slate-900 bg-white text-slate-900 hover:bg-slate-100 hover:-translate-y-0.5`

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

function IconInstagram() {
  return (
    <svg viewBox="0 0 24 24" fill="none" strokeWidth={1.8} stroke="currentColor" className="h-5 w-5">
      <rect x="3.5" y="3.5" width="17" height="17" rx="5" />
      <circle cx="12" cy="12" r="4" />
      <circle cx="17" cy="7" r="0.5" fill="currentColor" />
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

function IconSoccerBall() {
  return (
    <svg viewBox="0 0 24 24" fill="none" strokeWidth={1.6} stroke="currentColor" className="h-7 w-7">
      <circle cx="12" cy="12" r="8.25" />
      <path d="M12 8.5 9.7 10.2l.9 2.7h2.8l.9-2.7L12 8.5Z" />
      <path
        strokeLinecap="round"
        d="M12 8.5V4.75M9.7 10.2 6.35 7.85M14.3 10.2l3.35-2.35M10.6 12.9l-2 3.75M13.4 12.9l2 3.75"
      />
    </svg>
  )
}

function IconTennisRacket() {
  return (
    <svg viewBox="0 0 24 24" fill="none" strokeWidth={1.6} stroke="currentColor" className="h-7 w-7">
      <ellipse cx="13.2" cy="8.5" rx="4.5" ry="5.7" />
      <path strokeLinecap="round" d="M13.2 2.8v11.4M8.9 8.5h8.6" />
      <path strokeLinecap="round" d="M9.8 12.9 4.5 19.5" />
    </svg>
  )
}

function IconPadelRacket() {
  return (
    <svg viewBox="0 0 24 24" fill="none" strokeWidth={1.6} stroke="currentColor" className="h-7 w-7">
      <rect x="8" y="2.5" width="9.5" height="12.5" rx="4.5" />
      <path strokeLinecap="round" d="M9.8 12.9 4.5 19.5" />
      <circle cx="11.2" cy="6.3" r="0.6" fill="currentColor" stroke="none" />
      <circle cx="14.7" cy="6.3" r="0.6" fill="currentColor" stroke="none" />
      <circle cx="12.9" cy="9.2" r="0.6" fill="currentColor" stroke="none" />
      <circle cx="11.2" cy="12.1" r="0.6" fill="currentColor" stroke="none" />
      <circle cx="14.7" cy="12.1" r="0.6" fill="currentColor" stroke="none" />
    </svg>
  )
}

function IconVolleyball() {
  return (
    <svg viewBox="0 0 24 24" fill="none" strokeWidth={1.6} stroke="currentColor" className="h-7 w-7">
      <circle cx="12" cy="12" r="8.25" />
      <path strokeLinecap="round" d="M4.5 9.5c4-2.5 11-2.5 15 0M4.8 15c4.5 2 10 2 14.4 0M12 3.75v16.5" />
    </svg>
  )
}

function IconBasketball() {
  return (
    <svg viewBox="0 0 24 24" fill="none" strokeWidth={1.6} stroke="currentColor" className="h-7 w-7">
      <circle cx="12" cy="12" r="8.25" />
      <path strokeLinecap="round" d="M3.75 12h16.5M12 3.75v16.5M6 6c2.5 2.7 2.5 8.6 0 12M18 6c-2.5 2.7-2.5 8.6 0 12" />
    </svg>
  )
}

function IconCourt() {
  return (
    <svg viewBox="0 0 24 24" fill="none" strokeWidth={1.6} stroke="currentColor" className="h-7 w-7">
      <rect x="3.5" y="5" width="17" height="14" rx="2" />
      <path strokeLinecap="round" d="M12 5v14M3.5 12h5M15.5 12h5" />
      <circle cx="12" cy="12" r="2.2" />
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
  { nombre: 'Fútbol 5', icono: <IconSoccerBall /> },
  { nombre: 'Pádel', icono: <IconPadelRacket /> },
  { nombre: 'Tenis', icono: <IconTennisRacket /> },
  { nombre: 'Vóley', icono: <IconVolleyball /> },
  { nombre: 'Básquet', icono: <IconBasketball /> },
  { nombre: 'Otras canchas', icono: <IconCourt /> },
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
      className={`relative flex h-full flex-col gap-4 overflow-hidden rounded-xl border-2 bg-white p-6 shadow-soft transition-all duration-200 hover:-translate-y-1 hover:shadow-soft-lg ${
        plan.esPlanDePrueba ? 'border-slate-900' : 'border-slate-200'
      }`}
    >
      {plan.esPlanDePrueba && (
        <span className="absolute top-4 -right-9 w-32 rotate-45 bg-accent-500 py-1 text-center text-[11px] font-bold uppercase tracking-wide text-white shadow-soft">
          Para empezar
        </span>
      )}
      <h3 className="text-lg font-semibold text-slate-900">{plan.nombre}</h3>
      <p className="text-slate-900">
        <span className="text-3xl font-extrabold" style={{ fontFamily: 'var(--font-heading)' }}>
          ${plan.precio.toLocaleString('es-AR')}
        </span>
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
    <div className="bg-dotted flex min-h-svh flex-col">
      <header className="sticky top-0 z-20 border-b border-slate-200/80 bg-white/80 backdrop-blur-md">
        <div className="mx-auto flex max-w-6xl items-center justify-between px-4 py-3.5 sm:px-6">
          <Link to="/" className="group flex items-center gap-2">
            <img
              src="/logo.png"
              alt="MiTurno"
              className="h-9 w-9 rounded-xl object-cover shadow-soft transition-transform duration-200 group-hover:scale-105"
            />
            <span className="text-lg font-bold tracking-tight text-slate-900" style={{ fontFamily: 'var(--font-heading)' }}>
              Mi<span className="text-accent-500">Turno</span>
            </span>
          </Link>
          <nav className="flex items-center gap-2 sm:gap-6">
            <a
              href="#planes"
              className="hidden text-sm font-medium text-slate-600 transition-colors duration-200 hover:text-link-600 sm:inline"
            >
              Planes
            </a>
            <Link
              to="/panel/login"
              className="text-sm font-medium text-slate-600 transition-colors duration-200 hover:text-link-600"
            >
              Ingresar
            </Link>
            <Link to="/panel/registro" className={`${BOTON_PRIMARIO} hidden sm:inline-flex`}>
              Crear tu negocio
            </Link>
            <a
              href="https://www.instagram.com/miturno__/"
              target="_blank"
              rel="noopener noreferrer"
              aria-label="Instagram"
              className="ml-1 border-l border-slate-200 pl-3 text-slate-500 transition-colors duration-200 hover:text-link-600 sm:ml-2 sm:pl-4"
            >
              <IconInstagram />
            </a>
          </nav>
        </div>
      </header>

      <main className="flex-1">
        <section className="relative z-0 overflow-hidden">
          <div className="pointer-events-none absolute inset-0 -z-10 overflow-hidden">
            <div className="animate-blob absolute -top-32 -right-32 h-96 w-96 rounded-full bg-accent-100/70 blur-3xl" />
            <div className="animate-blob-delay absolute top-40 -left-32 h-72 w-72 rounded-full bg-link-50 blur-3xl" />
            <HeroCanchasFondo />
          </div>

          <div className="mx-auto grid max-w-6xl gap-12 px-4 py-16 sm:px-6 sm:py-24 lg:grid-cols-2 lg:items-center">
            <div className="animate-fade-in-up flex flex-col gap-7">
              <span className="inline-flex w-fit items-center gap-2 rounded-full border-2 border-slate-900 bg-white px-3 py-1 text-xs font-bold uppercase tracking-wide text-slate-900">
                <span className="h-1.5 w-1.5 rounded-full bg-accent-500" />
                Reservas online para tu cancha
              </span>
              <h1
                className="text-5xl leading-[1.05] font-extrabold tracking-tight text-slate-900 sm:text-6xl lg:text-7xl"
                style={{ fontFamily: 'var(--font-heading)' }}
              >
                Reservas de cancha,{' '}
                <span className="relative inline-block">
                  sin llamados ni WhatsApp perdido.
                  <svg
                    viewBox="0 0 300 12"
                    preserveAspectRatio="none"
                    className="absolute -bottom-1 left-0 h-3 w-full text-accent-400"
                  >
                    <path
                      d="M2 9c40-6 220-6 296 0"
                      fill="none"
                      stroke="currentColor"
                      strokeWidth="5"
                      strokeLinecap="round"
                    />
                  </svg>
                </span>
              </h1>
              <p className="max-w-lg text-lg text-slate-600">
                Compartí un link, tus clientes eligen día y horario disponible, pagan y quedan
                confirmados al instante. Vos mirás el panel y listo.
              </p>
              <div className="flex flex-wrap gap-3">
                <Link to="/panel/registro" className={`${BOTON_PRIMARIO} px-6 text-base`}>
                  Crear tu negocio gratis
                </Link>
                <a href="#planes" className={`${BOTON_SECUNDARIO} px-6 text-base`}>
                  Ver planes
                </a>
              </div>
            </div>

            <div className="flex justify-center lg:justify-end">
              <div className="relative w-full max-w-md">
                <div className="absolute inset-0 translate-x-3 translate-y-4 rotate-3 rounded-2xl border-2 border-slate-900/25 bg-white" />
                <div className="absolute inset-0 -translate-x-2 translate-y-6 -rotate-2 rounded-2xl border-2 border-slate-900/15 bg-white" />

                <span className="absolute -top-5 -right-5 z-10 flex h-16 w-16 -rotate-12 items-center justify-center rounded-full border-2 border-dashed border-accent-600 bg-accent-50 text-center text-[10px] font-black tracking-wide text-accent-700 uppercase">
                  Turno
                  <br />
                  confirmado
                </span>

                <div className="animate-card-float relative rounded-2xl border-2 border-slate-900 bg-white p-7 shadow-soft-lg">
                  <div className="flex items-center justify-between border-b-2 border-dashed border-slate-200 pb-4">
                    <div>
                      <p className="text-sm font-medium text-slate-500">Cancha Norte</p>
                      <p className="text-xl font-bold text-slate-900" style={{ fontFamily: 'var(--font-heading)' }}>
                        Fútbol 5
                      </p>
                    </div>
                    <span className="flex h-11 w-11 items-center justify-center rounded-full border-2 border-slate-900 bg-white text-slate-900">
                      <IconSoccerBall />
                    </span>
                  </div>

                  <div className="mt-4 flex items-center justify-between text-xs font-semibold text-slate-400">
                    {['L', 'M', 'M', 'J', 'V', 'S', 'D'].map((letra, i) => (
                      <span
                        key={`${letra}-${i}`}
                        className={`flex h-8 w-8 items-center justify-center rounded-full ${
                          i === 4 ? 'border-2 border-slate-900 bg-slate-900 text-white' : ''
                        }`}
                      >
                        {letra}
                      </span>
                    ))}
                  </div>

                  <p className="mt-4 text-sm font-medium text-slate-500">Viernes 20:00 - 21:00</p>
                  <div className="mt-2 grid grid-cols-3 gap-2 text-center text-sm">
                    <span className="rounded-lg border border-slate-200 py-2 text-slate-400 line-through">
                      18:00
                    </span>
                    <span className="rounded-lg border border-slate-200 py-2 text-slate-400 line-through">
                      19:00
                    </span>
                    <span className="rounded-lg border-2 border-accent-500 bg-accent-50 py-2 font-semibold text-accent-700">
                      20:00
                    </span>
                  </div>

                  <div className="mt-4 flex items-center gap-2.5 border-t-2 border-dashed border-slate-200 pt-4">
                    <span className="flex h-8 w-8 items-center justify-center rounded-full bg-link-50 text-xs font-bold text-link-700">
                      MG
                    </span>
                    <p className="text-sm text-slate-600">Martina G. reservó este turno</p>
                  </div>

                  <div className="mt-3 flex items-center justify-between">
                    <span className="text-sm text-slate-500">Total</span>
                    <span className="text-lg font-bold text-slate-900" style={{ fontFamily: 'var(--font-heading)' }}>
                      $5.000
                    </span>
                  </div>
                </div>
              </div>
            </div>
          </div>
        </section>

        <section id="como-funciona" className="bg-accent-300/25 px-4 py-20 sm:px-6">
          <div className="mx-auto max-w-6xl">
            <Reveal>
              <div className="flex flex-col items-center gap-3">
                <span className="rounded-full bg-slate-100 px-3 py-1 text-xs font-semibold tracking-wide text-slate-600 uppercase">
                  3 pasos simples
                </span>
                <h2
                  className="text-center text-3xl font-extrabold text-slate-900 sm:text-4xl"
                  style={{ fontFamily: 'var(--font-heading)' }}
                >
                  Cómo funciona
                </h2>
              </div>
            </Reveal>
            <div className="relative mt-14 grid gap-10 sm:grid-cols-3">
              <div className="pointer-events-none absolute top-6 right-0 left-0 hidden h-0.5 bg-slate-200 sm:block" />
              {PASOS.map((paso, i) => (
                <Reveal key={paso.titulo} delayMs={i * 100}>
                  <div className="relative flex h-full flex-col items-start gap-3 border-l-4 border-accent-500 pl-5 sm:border-l-0 sm:border-t-0 sm:pl-0">
                    <span
                      aria-hidden="true"
                      className="pointer-events-none absolute -top-8 right-0 text-7xl font-black text-slate-900/20 select-none sm:-top-10"
                      style={{ fontFamily: 'var(--font-heading)' }}
                    >
                      {i + 1}
                    </span>
                    <span className="relative z-10 flex h-11 w-11 items-center justify-center rounded-full border-2 border-slate-900 bg-white text-slate-900 shadow-soft sm:mt-2">
                      {paso.icono}
                    </span>
                    <h3 className="font-semibold text-slate-900">{paso.titulo}</h3>
                    <p className="text-sm text-slate-600">{paso.texto}</p>
                  </div>
                </Reveal>
              ))}
            </div>
          </div>
        </section>

        <section className="px-4 py-16 sm:px-6">
          <div className="mx-auto max-w-6xl">
            <Reveal>
              <h2 className="text-center text-2xl font-semibold text-slate-900 sm:text-3xl">
                Sirve para cualquier tipo de cancha
              </h2>
              <p className="mx-auto mt-2 max-w-xl text-center text-slate-600">
                Configurás tus recursos y horarios una vez, y funciona igual sea cual sea el deporte.
              </p>
            </Reveal>
            <div className="mt-10 grid grid-cols-2 gap-4 sm:grid-cols-3 lg:grid-cols-6">
              {DEPORTES.map((d, i) => (
                <Reveal key={d.nombre} delayMs={i * 60}>
                  <div className="group flex h-full flex-col items-center gap-3 rounded-xl border-2 border-slate-200 bg-white px-4 py-6 text-center shadow-soft transition-all duration-200 hover:-translate-y-1 hover:border-slate-900 hover:shadow-soft-lg">
                    <span className="flex h-16 w-16 items-center justify-center rounded-full bg-accent-50 text-accent-600 transition-colors duration-200 group-hover:bg-accent-500 group-hover:text-white">
                      {d.icono}
                    </span>
                    <span className="text-sm font-semibold text-slate-900">{d.nombre}</span>
                  </div>
                </Reveal>
              ))}
            </div>
          </div>
        </section>

        <section className="bg-dotted py-20">
          <div className="mx-auto max-w-6xl px-4 sm:px-6">
            <Reveal>
              <h2
                className="text-center text-3xl font-extrabold text-slate-900 sm:text-4xl"
                style={{ fontFamily: 'var(--font-heading)' }}
              >
                Todo lo que necesitás para dejar de anotar turnos a mano
              </h2>
            </Reveal>
            <div className="mt-10 grid gap-8 sm:grid-cols-2 lg:grid-cols-4">
              {BENEFICIOS.map((b, i) => (
                <Reveal key={b.titulo} delayMs={i * 80}>
                  <div
                    className={`group relative flex h-full flex-col gap-3 rounded-lg border-2 border-slate-900 bg-white p-6 shadow-soft transition-all duration-200 hover:-translate-y-1 hover:rotate-0 hover:shadow-soft-lg ${
                      i % 2 === 0 ? '-rotate-1' : 'rotate-1'
                    }`}
                  >
                    <span className="absolute -top-2.5 left-1/2 h-4 w-4 -translate-x-1/2 rounded-full border-2 border-slate-900 bg-accent-500 shadow-soft" />
                    <span className="flex h-9 w-9 items-center justify-center rounded-lg bg-accent-50 text-accent-600 transition-colors duration-200 group-hover:bg-accent-500 group-hover:text-white">
                      {b.icono}
                    </span>
                    <h3 className="font-semibold text-slate-900">{b.titulo}</h3>
                    <p className="text-sm text-slate-600">{b.texto}</p>
                  </div>
                </Reveal>
              ))}
            </div>
          </div>
        </section>

        <section id="planes" className="bg-accent-300/25 px-4 py-20 sm:px-6">
          <div className="mx-auto max-w-6xl">
            <Reveal>
              <h2
                className="text-center text-3xl font-extrabold text-slate-900 sm:text-4xl"
                style={{ fontFamily: 'var(--font-heading)' }}
              >
                Planes
              </h2>
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

        <section className="bg-dotted py-20">
          <div className="mx-auto max-w-3xl px-4 sm:px-6">
            <Reveal>
              <h2
                className="text-center text-3xl font-extrabold text-slate-900 sm:text-4xl"
                style={{ fontFamily: 'var(--font-heading)' }}
              >
                Preguntas frecuentes
              </h2>
            </Reveal>
            <div className="mt-10 overflow-hidden rounded-xl border-2 border-slate-900 bg-white">
              {FAQS.map((f, i) => (
                <details
                  key={f.pregunta}
                  className={`group px-5 py-4 open:bg-slate-50 sm:px-6 ${i > 0 ? 'border-t-2 border-slate-900' : ''}`}
                >
                  <summary className="flex cursor-pointer list-none items-center justify-between gap-4">
                    <span className="flex items-center gap-4">
                      <span
                        className="text-sm font-black text-slate-200"
                        style={{ fontFamily: 'var(--font-heading)' }}
                        aria-hidden="true"
                      >
                        {String(i + 1).padStart(2, '0')}
                      </span>
                      <span className="font-medium text-slate-900">{f.pregunta}</span>
                    </span>
                    <span className="shrink-0 text-accent-500 transition-transform duration-200 group-open:rotate-45">
                      <svg viewBox="0 0 24 24" fill="none" strokeWidth={2} stroke="currentColor" className="h-5 w-5">
                        <path strokeLinecap="round" strokeLinejoin="round" d="M12 4.5v15m7.5-7.5h-15" />
                      </svg>
                    </span>
                  </summary>
                  <p className="mt-3 pl-9 text-sm text-slate-600">{f.respuesta}</p>
                </details>
              ))}
            </div>
          </div>
        </section>

        <section
          className="relative overflow-hidden bg-cover bg-center"
          style={{
            backgroundImage:
              'linear-gradient(to right, rgba(217,122,59,0.94), rgba(193,99,42,0.94)), url(/images/cancha-hero.jpg)',
          }}
        >
          <Reveal>
            <div className="mx-auto flex max-w-6xl flex-col items-center gap-4 px-4 py-20 text-center sm:px-6">
              <h2
                className="text-3xl font-extrabold text-white sm:text-4xl"
                style={{ fontFamily: 'var(--font-heading)' }}
              >
                ¿Tenés una cancha y querés recibir reservas online?
              </h2>
              <p className="max-w-xl text-accent-50">
                Creá tu cuenta en un minuto y compartí tu link hoy mismo.
              </p>
              <Link
                to="/panel/registro"
                className={`${BOTON} border-2 border-slate-900 bg-white px-6 text-base text-accent-700 shadow-soft-lg hover:-translate-y-0.5 hover:bg-accent-50`}
              >
                Crear tu negocio gratis
              </Link>
            </div>
          </Reveal>
        </section>
      </main>

      <footer className="relative overflow-hidden border-t border-slate-200 py-10">
        <p
          aria-hidden="true"
          className="pointer-events-none absolute -bottom-10 left-1/2 hidden -translate-x-1/2 text-[10rem] leading-none font-black whitespace-nowrap text-slate-100 select-none sm:block"
          style={{ fontFamily: 'var(--font-heading)' }}
        >
          MiTurno
        </p>
        <div className="relative mx-auto flex max-w-6xl flex-col items-center gap-6 px-4 sm:flex-row sm:items-start sm:justify-between sm:px-6">
          <div>
            <p className="text-lg font-bold tracking-tight text-slate-900" style={{ fontFamily: 'var(--font-heading)' }}>
              Mi<span className="text-accent-500">Turno</span>
            </p>
            <p className="mt-1 text-sm text-slate-500">Reservá tu turno en segundos.</p>
          </div>
          <div className="flex flex-col gap-1 text-sm sm:items-end">
            <p className="font-medium text-slate-700">¿Necesitás ayuda? Escribinos</p>
            <a href="tel:+543412853608" className="text-link-600 hover:text-link-700 hover:underline">
              Tel / WhatsApp: 341 285-3608
            </a>
            <a
              href="https://www.instagram.com/miturno__/"
              target="_blank"
              rel="noopener noreferrer"
              className="inline-flex items-center gap-1.5 text-link-600 hover:text-link-700 hover:underline"
            >
              <IconInstagram />
              @miturno__
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
