import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { listarClientes } from '../../api/clientes'
import { extractError } from '../../api/client'
import type { Cliente } from '../../types/cliente'
import { Card } from '../../components/Card'
import { Spinner } from '../../components/Spinner'
import { ErrorBanner } from '../../components/ErrorBanner'

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
            <Link key={cliente.id} to={`/panel/clientes/${cliente.id}`}>
              <Card className="flex flex-col gap-1 transition-shadow hover:shadow-md sm:flex-row sm:items-center sm:justify-between">
                <div>
                  <p className="font-semibold text-slate-900">{cliente.nombre}</p>
                  <p className="text-sm text-slate-500">
                    {cliente.email}
                    {cliente.telefono && ` · ${cliente.telefono}`}
                  </p>
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
