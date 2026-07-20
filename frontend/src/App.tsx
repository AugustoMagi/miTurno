import { Navigate, Route, Routes } from 'react-router-dom'
import { Layout } from './components/Layout'
import { NegocioPage } from './pages/NegocioPage'
import { ReservaWizardPage } from './pages/ReservaWizardPage'
import { NotFoundPage } from './pages/NotFoundPage'
import { RequireAuth } from './components/RequireAuth'
import { PanelLayout } from './components/PanelLayout'
import { LoginPage } from './pages/panel/LoginPage'
import { RegisterPage } from './pages/panel/RegisterPage'
import { RecursosListPage } from './pages/panel/RecursosListPage'
import { RecursoDetailPage } from './pages/panel/RecursoDetailPage'
import { ReservasListPage } from './pages/panel/ReservasListPage'
import { ClientesListPage } from './pages/panel/ClientesListPage'
import { ClienteDetailPage } from './pages/panel/ClienteDetailPage'
import { ConfiguracionPagoPage } from './pages/panel/ConfiguracionPagoPage'
import { EstadisticasPage } from './pages/panel/EstadisticasPage'
import { MiSuscripcionPage } from './pages/panel/MiSuscripcionPage'
import { RequireAdminAuth } from './components/RequireAdminAuth'
import { AdminLayout } from './components/AdminLayout'
import { AdminLoginPage } from './pages/admin/AdminLoginPage'
import { PlanesPage } from './pages/admin/PlanesPage'
import { SuscripcionesPage } from './pages/admin/SuscripcionesPage'

function App() {
  return (
    <Routes>
      <Route path="/admin/login" element={<AdminLoginPage />} />
      <Route path="/admin" element={<RequireAdminAuth />}>
        <Route element={<AdminLayout />}>
          <Route index element={<Navigate to="/admin/planes" replace />} />
          <Route path="planes" element={<PlanesPage />} />
          <Route path="suscripciones" element={<SuscripcionesPage />} />
        </Route>
      </Route>

      <Route path="/panel/login" element={<LoginPage />} />
      <Route path="/panel/registro" element={<RegisterPage />} />
      <Route path="/panel" element={<RequireAuth />}>
        <Route element={<PanelLayout />}>
          <Route index element={<Navigate to="/panel/estadisticas" replace />} />
          <Route path="estadisticas" element={<EstadisticasPage />} />
          <Route path="recursos" element={<RecursosListPage />} />
          <Route path="recursos/:id" element={<RecursoDetailPage />} />
          <Route path="reservas" element={<ReservasListPage />} />
          <Route path="clientes" element={<ClientesListPage />} />
          <Route path="clientes/:id" element={<ClienteDetailPage />} />
          <Route path="configuracion-pago" element={<ConfiguracionPagoPage />} />
          <Route path="suscripcion" element={<MiSuscripcionPage />} />
        </Route>
      </Route>

      <Route
        path="/"
        element={
          <Layout>
            <NotFoundPage />
          </Layout>
        }
      />
      <Route
        path="/:slug"
        element={
          <Layout>
            <NegocioPage />
          </Layout>
        }
      />
      <Route
        path="/:slug/reservar/:recursoId"
        element={
          <Layout>
            <ReservaWizardPage />
          </Layout>
        }
      />
      <Route
        path="*"
        element={
          <Layout>
            <NotFoundPage />
          </Layout>
        }
      />
    </Routes>
  )
}

export default App
