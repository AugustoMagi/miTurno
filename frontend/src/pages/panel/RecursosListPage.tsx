import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { activarRecurso, crearRecurso, desactivarRecurso, listarRecursos } from '../../api/recursos'
import { extractError } from '../../api/client'
import type { Recurso } from '../../types/recurso'
import { Button } from '../../components/Button'
import { Card } from '../../components/Card'
import { Spinner } from '../../components/Spinner'
import { ErrorBanner } from '../../components/ErrorBanner'

export function RecursosListPage() {
  const [recursos, setRecursos] = useState<Recurso[] | null>(null)
  const [error, setError] = useState<string | null>(null)

  const [mostrarForm, setMostrarForm] = useState(false)
  const [nombre, setNombre] = useState('')
  const [tipo, setTipo] = useState('')
  const [duracionTurnoMinutos, setDuracionTurnoMinutos] = useState(60)
  const [precio, setPrecio] = useState(0)
  const [creando, setCreando] = useState(false)
  const [formError, setFormError] = useState<string | null>(null)

  const [cambiandoEstado, setCambiandoEstado] = useState<string | null>(null)

  function cargar() {
    setError(null)
    listarRecursos()
      .then(setRecursos)
      .catch((err) => setError(extractError(err)))
  }

  useEffect(cargar, [])

  async function handleCrear(event: React.FormEvent) {
    event.preventDefault()
    setCreando(true)
    setFormError(null)
    try {
      await crearRecurso({ nombre, tipo, duracionTurnoMinutos, precio })
      setNombre('')
      setTipo('')
      setDuracionTurnoMinutos(60)
      setPrecio(0)
      setMostrarForm(false)
      cargar()
    } catch (err) {
      setFormError(extractError(err))
    } finally {
      setCreando(false)
    }
  }

  async function handleCambiarEstado(recurso: Recurso) {
    setCambiandoEstado(recurso.id)
    try {
      if (recurso.activo) await desactivarRecurso(recurso.id)
      else await activarRecurso(recurso.id)
      cargar()
    } catch (err) {
      setError(extractError(err))
    } finally {
      setCambiandoEstado(null)
    }
  }

  return (
    <div className="flex flex-col gap-6">
      <div className="flex items-center justify-between">
        <h1 className="text-xl font-semibold text-slate-900">Recursos</h1>
        <Button onClick={() => setMostrarForm((show) => !show)}>
          {mostrarForm ? 'Cancelar' : 'Nuevo recurso'}
        </Button>
      </div>

      {mostrarForm && (
        <Card>
          <form className="flex flex-col gap-4" onSubmit={handleCrear}>
            <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
              <label className="flex flex-col gap-1 text-sm font-medium text-slate-700">
                Nombre
                <input
                  type="text"
                  required
                  maxLength={150}
                  className="rounded-lg border border-slate-300 px-3 py-2 text-sm focus:border-emerald-500 focus:outline-none"
                  value={nombre}
                  onChange={(event) => setNombre(event.target.value)}
                />
              </label>
              <label className="flex flex-col gap-1 text-sm font-medium text-slate-700">
                Tipo
                <input
                  type="text"
                  required
                  placeholder="Futbol 5, Padel, ..."
                  className="rounded-lg border border-slate-300 px-3 py-2 text-sm focus:border-emerald-500 focus:outline-none"
                  value={tipo}
                  onChange={(event) => setTipo(event.target.value)}
                />
              </label>
              <label className="flex flex-col gap-1 text-sm font-medium text-slate-700">
                Duración del turno (min)
                <input
                  type="number"
                  required
                  min={1}
                  className="rounded-lg border border-slate-300 px-3 py-2 text-sm focus:border-emerald-500 focus:outline-none"
                  value={duracionTurnoMinutos}
                  onChange={(event) => setDuracionTurnoMinutos(Number(event.target.value))}
                />
              </label>
              <label className="flex flex-col gap-1 text-sm font-medium text-slate-700">
                Precio
                <input
                  type="number"
                  required
                  min={0}
                  step="0.01"
                  className="rounded-lg border border-slate-300 px-3 py-2 text-sm focus:border-emerald-500 focus:outline-none"
                  value={precio}
                  onChange={(event) => setPrecio(Number(event.target.value))}
                />
              </label>
            </div>
            {formError && <ErrorBanner message={formError} />}
            <Button type="submit" disabled={creando} className="self-start">
              {creando ? 'Creando…' : 'Crear recurso'}
            </Button>
          </form>
        </Card>
      )}

      {error && <ErrorBanner message={error} />}

      {!recursos ? (
        <Spinner />
      ) : recursos.length === 0 ? (
        <p className="text-slate-500">Todavía no cargaste ningún recurso.</p>
      ) : (
        <div className="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-3">
          {recursos.map((recurso) => (
            <Card key={recurso.id} className="flex flex-col gap-3">
              <div className="flex items-start justify-between gap-2">
                <div>
                  <Link to={`/panel/recursos/${recurso.id}`} className="font-semibold text-slate-900 hover:underline">
                    {recurso.nombre}
                  </Link>
                  <p className="text-sm text-slate-500">{recurso.tipo}</p>
                </div>
                <span
                  className={`rounded-full px-2 py-0.5 text-xs font-medium ${
                    recurso.activo ? 'bg-emerald-50 text-emerald-700' : 'bg-slate-100 text-slate-500'
                  }`}
                >
                  {recurso.activo ? 'Activo' : 'Inactivo'}
                </span>
              </div>
              <div className="flex items-center justify-between text-sm">
                <span className="text-slate-500">{recurso.duracionTurnoMinutos} min</span>
                <span className="font-semibold text-emerald-700">
                  ${recurso.precio.toLocaleString('es-AR')}
                </span>
              </div>
              <div className="flex gap-2">
                <Link to={`/panel/recursos/${recurso.id}`} className="flex-1">
                  <Button variant="secondary" className="w-full">
                    Administrar
                  </Button>
                </Link>
                <Button
                  variant="secondary"
                  disabled={cambiandoEstado === recurso.id}
                  onClick={() => handleCambiarEstado(recurso)}
                >
                  {recurso.activo ? 'Desactivar' : 'Activar'}
                </Button>
              </div>
            </Card>
          ))}
        </div>
      )}
    </div>
  )
}
