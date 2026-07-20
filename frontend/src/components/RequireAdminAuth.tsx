import { Navigate, Outlet } from 'react-router-dom'
import { useAdminAuth } from '../context/AdminAuthContext'

export function RequireAdminAuth() {
  const { sesion } = useAdminAuth()

  if (!sesion) {
    return <Navigate to="/admin/login" replace />
  }

  return <Outlet />
}
