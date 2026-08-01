import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { listarClientes } from '../../api/clientes'
import { extractError } from '../../api/client'
import type { Cliente } from '../../types/cliente'
import { Card } from '../../components/Card'
import { Spinner } from '../../components/Spinner'
import { ErrorBanner } from '../../components/ErrorBanner'

function iniciales(nombre: string): string {
  const partes = nombre.trim().split(/\s+/)
  return ((partes[0]?.[0] ?? '') + (partes[1]?.[0] ?? '')).toUpperCase()
}

export function ClientesListPage() {
  const [clientes, setClientes] = useState<Cliente[] | null>(null)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    listarClientes()
      .then(setClientes)
      .catch((err) => setError(extractError(err)))
  }, [])

  if (error) return <ErrorBanner message={error} />
  if (!clientes) return <Spinner />

  return (
    <div className="flex flex-col gap-6">
      <h1 className="text-xl font-semibold text-slate-900">Clientes</h1>

      {clientes.length === 0 ? (
        <p className="text-slate-500">Todavía no tenés clientes.</p>
      ) : (
        <div className="flex flex-col gap-3">
          {clientes.map((cliente) => (
            <Link key={cliente.id} to={`/panel/clientes/${cliente.id}`} className="group">
              <Card hover className="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
                <div className="flex items-center gap-3">
                  <span className="flex h-10 w-10 shrink-0 items-center justify-center rounded-full bg-link-50 text-sm font-semibold text-link-700">
                    {iniciales(cliente.nombre)}
                  </span>
                  <div>
                    <p className="font-semibold text-slate-900 transition-colors duration-200 group-hover:text-link-700">
                      {cliente.nombre}
                    </p>
                    <p className="text-sm text-slate-500">
                      {cliente.email}
                      {cliente.telefono && ` · ${cliente.telefono}`}
                    </p>
                  </div>
                </div>
                <div className="text-sm text-slate-500 sm:text-right">
                  <p>{cliente.totalReservas} reserva(s)</p>
                  <p>Última: {cliente.ultimaReserva}</p>
                </div>
              </Card>
            </Link>
          ))}
        </div>
      )}
    </div>
  )
}
