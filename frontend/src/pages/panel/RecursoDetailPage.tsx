import { useEffect, useState } from 'react'
import { Link, useParams } from 'react-router-dom'
import { obtenerRecurso, actualizarRecurso } from '../../api/recursos'
import { agregarHorario, eliminarHorario, listarHorarios } from '../../api/horarios'
import { agregarBloqueo, eliminarBloqueo, listarBloqueos } from '../../api/bloqueos'
import { extractError } from '../../api/client'
import type { Recurso } from '../../types/recurso'
import type { HorarioDisponible } from '../../types/horario'
import { DIAS_SEMANA } from '../../types/horario'
import type { BloqueoFecha } from '../../types/bloqueo'
import { Button } from '../../components/Button'
import { Card } from '../../components/Card'
import { Spinner } from '../../components/Spinner'
import { ErrorBanner } from '../../components/ErrorBanner'
import { Field, Input, Select } from '../../components/Input'
import { AlertTriangleIcon, ArrowLeftIcon, CheckIcon, TrashIcon } from '../../components/icons'
import {
  validarEntero,
  validarNumeroNoNegativo,
  validarRangoHorario,
  validarRequerido,
} from '../../utils/validation'

function formatHora(horaHms: string): string {
  return horaHms.slice(0, 5)
}

function nombreDia(diaSemana: number): string {
  return DIAS_SEMANA.find((d) => d.valor === diaSemana)?.nombre ?? String(diaSemana)
}

export function RecursoDetailPage() {
  const { id } = useParams<{ id: string }>()

  const [recurso, setRecurso] = useState<Recurso | null>(null)
  const [cargando, setCargando] = useState(true)
  const [error, setError] = useState<string | null>(null)

  const [nombre, setNombre] = useState('')
  const [tipo, setTipo] = useState('')
  const [duracionTurnoMinutos, setDuracionTurnoMinutos] = useState(60)
  const [precio, setPrecio] = useState(0)
  const [guardando, setGuardando] = useState(false)
  const [guardadoError, setGuardadoError] = useState<string | null>(null)
  const [guardadoOk, setGuardadoOk] = useState(false)
  const [datosTocado, setDatosTocado] = useState<Record<string, boolean>>({})

  const [horarios, setHorarios] = useState<HorarioDisponible[] | null>(null)
  const [diaSemana, setDiaSemana] = useState(1)
  const [horaInicio, setHoraInicio] = useState('08:00')
  const [horaFin, setHoraFin] = useState('22:00')
  const [agregandoHorario, setAgregandoHorario] = useState(false)
  const [horarioError, setHorarioError] = useState<string | null>(null)

  const [bloqueos, setBloqueos] = useState<BloqueoFecha[] | null>(null)
  const [fechaBloqueo, setFechaBloqueo] = useState('')
  const [horaInicioBloqueo, setHoraInicioBloqueo] = useState('')
  const [horaFinBloqueo, setHoraFinBloqueo] = useState('')
  const [motivoBloqueo, setMotivoBloqueo] = useState('')
  const [agregandoBloqueo, setAgregandoBloqueo] = useState(false)
  const [bloqueoError, setBloqueoError] = useState<string | null>(null)
  const [ultimoBloqueoAfectados, setUltimoBloqueoAfectados] = useState<BloqueoFecha | null>(null)

  function cargarHorarios(recursoId: string) {
    listarHorarios(recursoId)
      .then(setHorarios)
      .catch((err) => setHorarioError(extractError(err)))
  }

  function cargarBloqueos(recursoId: string) {
    listarBloqueos(recursoId)
      .then(setBloqueos)
      .catch((err) => setBloqueoError(extractError(err)))
  }

  useEffect(() => {
    if (!id) return
    setCargando(true)
    obtenerRecurso(id)
      .then((data) => {
        setRecurso(data)
        setNombre(data.nombre)
        setTipo(data.tipo)
        setDuracionTurnoMinutos(data.duracionTurnoMinutos)
        setPrecio(data.precio)
      })
      .catch((err) => setError(extractError(err)))
      .finally(() => setCargando(false))

    cargarHorarios(id)
    cargarBloqueos(id)
  }, [id])

  const errorNombre = validarRequerido(nombre, 'El nombre')
  const errorTipo = validarRequerido(tipo, 'El tipo')
  const errorDuracion = validarEntero(duracionTurnoMinutos, 'La duración del turno', 1)
  const errorPrecio = validarNumeroNoNegativo(precio, 'El precio')
  const errorRangoHorario = validarRangoHorario(horaInicio, horaFin)

  const errorHorarioBloqueo = horaInicioBloqueo || horaFinBloqueo
    ? !horaInicioBloqueo || !horaFinBloqueo
      ? 'Completá el horario "desde" y "hasta", o dejá ambos vacíos para bloquear el día completo.'
      : validarRangoHorario(horaInicioBloqueo, horaFinBloqueo)
    : undefined

  async function handleGuardar(event: React.FormEvent) {
    event.preventDefault()
    if (!id) return
    setDatosTocado({ nombre: true, tipo: true, duracion: true, precio: true })
    if (errorNombre || errorTipo || errorDuracion || errorPrecio) return
    setGuardando(true)
    setGuardadoError(null)
    setGuardadoOk(false)
    try {
      const actualizado = await actualizarRecurso(id, { nombre, tipo, duracionTurnoMinutos, precio })
      setRecurso(actualizado)
      setGuardadoOk(true)
    } catch (err) {
      setGuardadoError(extractError(err))
    } finally {
      setGuardando(false)
    }
  }

  async function handleAgregarHorario(event: React.FormEvent) {
    event.preventDefault()
    if (!id) return
    if (errorRangoHorario) {
      setHorarioError(errorRangoHorario)
      return
    }
    setAgregandoHorario(true)
    setHorarioError(null)
    try {
      await agregarHorario(id, { diaSemana, horaInicio: `${horaInicio}:00`, horaFin: `${horaFin}:00` })
      cargarHorarios(id)
    } catch (err) {
      setHorarioError(extractError(err))
    } finally {
      setAgregandoHorario(false)
    }
  }

  async function handleEliminarHorario(horarioId: string) {
    if (!id) return
    try {
      await eliminarHorario(id, horarioId)
      cargarHorarios(id)
    } catch (err) {
      setHorarioError(extractError(err))
    }
  }

  async function handleAgregarBloqueo(event: React.FormEvent) {
    event.preventDefault()
    if (!id) return
    if (!fechaBloqueo) {
      setBloqueoError('La fecha es obligatoria.')
      return
    }
    if (errorHorarioBloqueo) {
      setBloqueoError(errorHorarioBloqueo)
      return
    }
    setAgregandoBloqueo(true)
    setBloqueoError(null)
    setUltimoBloqueoAfectados(null)
    try {
      const bloqueo = await agregarBloqueo(id, {
        fecha: fechaBloqueo,
        motivo: motivoBloqueo || undefined,
        horaInicio: horaInicioBloqueo ? `${horaInicioBloqueo}:00` : undefined,
        horaFin: horaFinBloqueo ? `${horaFinBloqueo}:00` : undefined,
      })
      if (bloqueo.reservasAfectadas.length > 0) setUltimoBloqueoAfectados(bloqueo)
      setFechaBloqueo('')
      setHoraInicioBloqueo('')
      setHoraFinBloqueo('')
      setMotivoBloqueo('')
      cargarBloqueos(id)
    } catch (err) {
      setBloqueoError(extractError(err))
    } finally {
      setAgregandoBloqueo(false)
    }
  }

  async function handleEliminarBloqueo(bloqueoId: string) {
    if (!id) return
    try {
      await eliminarBloqueo(id, bloqueoId)
      cargarBloqueos(id)
    } catch (err) {
      setBloqueoError(extractError(err))
    }
  }

  if (cargando) return <Spinner />
  if (error || !recurso) return <ErrorBanner message={error ?? 'Recurso no encontrado.'} />

  return (
    <div className="flex flex-col gap-6">
      <div>
        <Link
          to="/panel/recursos"
          className="inline-flex items-center gap-1 text-sm font-medium text-link-600 hover:text-link-700"
        >
          <ArrowLeftIcon className="h-4 w-4" />
          Recursos
        </Link>
        <h1 className="mt-2 text-xl font-semibold text-slate-900">{recurso.nombre}</h1>
      </div>

      <Card>
        <form className="flex flex-col gap-4" onSubmit={handleGuardar}>
          <h2 className="font-semibold text-slate-900">Datos</h2>
          <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
            <Field label="Nombre" error={datosTocado.nombre ? errorNombre : undefined} required>
              <Input
                type="text"
                required
                maxLength={150}
                value={nombre}
                onChange={(event) => setNombre(event.target.value)}
                onBlur={() => setDatosTocado((t) => ({ ...t, nombre: true }))}
                aria-invalid={Boolean(datosTocado.nombre && errorNombre)}
              />
            </Field>
            <Field label="Tipo" error={datosTocado.tipo ? errorTipo : undefined} required>
              <Input
                type="text"
                required
                value={tipo}
                onChange={(event) => setTipo(event.target.value)}
                onBlur={() => setDatosTocado((t) => ({ ...t, tipo: true }))}
                aria-invalid={Boolean(datosTocado.tipo && errorTipo)}
              />
            </Field>
            <Field label="Duración del turno (min)" error={datosTocado.duracion ? errorDuracion : undefined} required>
              <Input
                type="number"
                required
                min={1}
                value={duracionTurnoMinutos}
                onChange={(event) => setDuracionTurnoMinutos(Number(event.target.value))}
                onBlur={() => setDatosTocado((t) => ({ ...t, duracion: true }))}
                aria-invalid={Boolean(datosTocado.duracion && errorDuracion)}
              />
            </Field>
            <Field label="Precio" error={datosTocado.precio ? errorPrecio : undefined} required>
              <Input
                type="number"
                required
                min={0}
                step="0.01"
                value={precio}
                onChange={(event) => setPrecio(Number(event.target.value))}
                onBlur={() => setDatosTocado((t) => ({ ...t, precio: true }))}
                aria-invalid={Boolean(datosTocado.precio && errorPrecio)}
              />
            </Field>
          </div>
          {guardadoError && <ErrorBanner message={guardadoError} />}
          {guardadoOk && (
            <p className="flex items-center gap-1.5 text-sm font-medium text-emerald-700">
              <CheckIcon className="h-4 w-4" />
              Guardado.
            </p>
          )}
          <Button type="submit" loading={guardando} className="self-start">
            Guardar cambios
          </Button>
        </form>
      </Card>

      <Card>
        <h2 className="font-semibold text-slate-900">Horarios disponibles</h2>
        <form className="mt-4 flex flex-wrap items-end gap-3" onSubmit={handleAgregarHorario}>
          <Field label="Día">
            <Select value={diaSemana} onChange={(event) => setDiaSemana(Number(event.target.value))} className="w-auto">
              {DIAS_SEMANA.map((dia) => (
                <option key={dia.valor} value={dia.valor}>
                  {dia.nombre}
                </option>
              ))}
            </Select>
          </Field>
          <Field label="Desde">
            <Input type="time" value={horaInicio} onChange={(event) => setHoraInicio(event.target.value)} className="w-auto" />
          </Field>
          <Field label="Hasta">
            <Input type="time" value={horaFin} onChange={(event) => setHoraFin(event.target.value)} className="w-auto" />
          </Field>
          <Button type="submit" loading={agregandoHorario}>
            Agregar
          </Button>
        </form>
        {horarioError && (
          <div className="mt-3">
            <ErrorBanner message={horarioError} />
          </div>
        )}
        <div className="mt-4 flex flex-col gap-2">
          {!horarios ? (
            <Spinner />
          ) : horarios.length === 0 ? (
            <p className="text-sm text-slate-500">Sin horarios cargados.</p>
          ) : (
            horarios
              .slice()
              .sort((a, b) => a.diaSemana - b.diaSemana || a.horaInicio.localeCompare(b.horaInicio))
              .map((horario) => (
                <div
                  key={horario.id}
                  className="flex items-center justify-between rounded-xl border border-slate-200 px-4 py-2.5 text-sm transition-colors duration-200 hover:bg-slate-50"
                >
                  <span>
                    <span className="font-medium text-slate-900">{nombreDia(horario.diaSemana)}</span>{' '}
                    <span className="text-slate-500">
                      {formatHora(horario.horaInicio)} - {formatHora(horario.horaFin)}
                    </span>
                  </span>
                  <button
                    type="button"
                    onClick={() => handleEliminarHorario(horario.id)}
                    aria-label="Eliminar horario"
                    className="flex h-8 w-8 items-center justify-center rounded-lg text-red-500 transition-colors duration-200 hover:bg-red-50 hover:text-red-700"
                  >
                    <TrashIcon className="h-4 w-4" />
                  </button>
                </div>
              ))
          )}
        </div>
      </Card>

      <Card>
        <h2 className="font-semibold text-slate-900">Bloqueos de fecha y horario</h2>
        <p className="mt-1 text-sm text-slate-500">
          Bloqueá una fecha completa (feriado, mantenimiento) o solo un rango horario puntual — por
          ejemplo, si ya te reservaron esa cancha por teléfono o WhatsApp y no querés que se pueda
          reservar online también.
        </p>
        <form className="mt-4 flex flex-wrap items-end gap-3" onSubmit={handleAgregarBloqueo}>
          <Field label="Fecha">
            <Input
              type="date"
              required
              value={fechaBloqueo}
              onChange={(event) => setFechaBloqueo(event.target.value)}
              className="w-auto"
            />
          </Field>
          <Field label="Desde (opcional)">
            <Input
              type="time"
              value={horaInicioBloqueo}
              onChange={(event) => setHoraInicioBloqueo(event.target.value)}
              className="w-auto"
            />
          </Field>
          <Field label="Hasta (opcional)" error={errorHorarioBloqueo}>
            <Input
              type="time"
              value={horaFinBloqueo}
              onChange={(event) => setHoraFinBloqueo(event.target.value)}
              className="w-auto"
            />
          </Field>
          <Field label="Motivo (opcional)">
            <Input
              type="text"
              value={motivoBloqueo}
              onChange={(event) => setMotivoBloqueo(event.target.value)}
            />
          </Field>
          <Button type="submit" loading={agregandoBloqueo}>
            Bloquear
          </Button>
        </form>
        <p className="mt-2 text-xs text-slate-400">
          Dejá "Desde" y "Hasta" vacíos para bloquear el día completo.
        </p>
        {bloqueoError && (
          <div className="mt-3">
            <ErrorBanner message={bloqueoError} />
          </div>
        )}
        {ultimoBloqueoAfectados && (
          <div className="animate-scale-in mt-3 flex gap-2.5 rounded-xl border border-amber-200 bg-amber-50 px-4 py-3 text-sm text-amber-800">
            <AlertTriangleIcon className="mt-0.5 h-4 w-4 shrink-0" />
            <div>
              <p className="font-medium">
                Ojo: {ultimoBloqueoAfectados.reservasAfectadas.length} reserva(s) activa(s) quedaron en esta fecha.
                Avisales a estos clientes:
              </p>
              <ul className="mt-1 list-inside list-disc">
                {ultimoBloqueoAfectados.reservasAfectadas.map((r) => (
                  <li key={r.id}>
                    {r.clienteNombre} ({r.clienteEmail}) — {formatHora(r.horaInicio)} a {formatHora(r.horaFin)}
                  </li>
                ))}
              </ul>
            </div>
          </div>
        )}
        <div className="mt-4 flex flex-col gap-2">
          {!bloqueos ? (
            <Spinner />
          ) : bloqueos.length === 0 ? (
            <p className="text-sm text-slate-500">Sin fechas bloqueadas.</p>
          ) : (
            bloqueos.map((bloqueo) => (
              <div
                key={bloqueo.id}
                className="flex items-center justify-between rounded-xl border border-slate-200 px-4 py-2.5 text-sm transition-colors duration-200 hover:bg-slate-50"
              >
                <span>
                  <span className="font-medium text-slate-900">{bloqueo.fecha}</span>{' '}
                  {bloqueo.horaInicio && bloqueo.horaFin ? (
                    <span className="text-slate-500">
                      {formatHora(bloqueo.horaInicio)} - {formatHora(bloqueo.horaFin)}
                    </span>
                  ) : (
                    <span className="rounded-full bg-slate-100 px-2 py-0.5 text-xs font-medium text-slate-600">
                      Día completo
                    </span>
                  )}{' '}
                  {bloqueo.motivo && <span className="text-slate-500">— {bloqueo.motivo}</span>}
                </span>
                <button
                  type="button"
                  onClick={() => handleEliminarBloqueo(bloqueo.id)}
                  aria-label="Eliminar bloqueo"
                  className="flex h-8 w-8 items-center justify-center rounded-lg text-red-500 transition-colors duration-200 hover:bg-red-50 hover:text-red-700"
                >
                  <TrashIcon className="h-4 w-4" />
                </button>
              </div>
            ))
          )}
        </div>
      </Card>
    </div>
  )
}
