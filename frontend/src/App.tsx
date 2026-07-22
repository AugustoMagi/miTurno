import { Navigate, Route, Routes } from 'react-router-dom'
import { Layout } from './components/Layout'
import { LandingPage } from './pages/LandingPage'
import { NegocioPage } from './pages/NegocioPage'
import { ReservaWizardPage } from './pages/ReservaWizardPage'
import { NotFoundPage } from './pages/NotFoundPage'
import { RequireAuth } from './components/RequireAuth'
import { PanelLayout } from './components/PanelLayout'
import { LoginPage } from './pages/panel/LoginPage'
import { RegisterPage } from './pages/panel/RegisterPage'
import { OlvidePasswordPage } from './pages/panel/OlvidePasswordPage'
import { RestablecerPasswordPage } from './pages/panel/RestablecerPasswordPage'
import { MiNegocioPage } from './pages/panel/MiNegocioPage'
import { RecursosListPage } from './pages/panel/RecursosListPage'
import { RecursoDetailPage } from './pages/panel/RecursoDetailPage'
import { ReservasListPage } from './pages/panel/ReservasListPage'
import { ClientesListPage } from './pages/panel/ClientesListPage'
import { ClienteDetailPage } from './pages/panel/ClienteDetailPage'
import { ConfiguracionPagoPage } from './pages/panel/ConfiguracionPagoPage'
import { EstadisticasPage } from './pages/panel/EstadisticasPage'
import { MiSuscripcionPage } from './pages/panel/MiSuscripcionPage'
import { PerfilPage } from './pages/panel/PerfilPage'
import { RequireAdminAuth } from './components/RequireAdminAuth'
import { AdminLayout } from './components/AdminLayout'
import { AdminLoginPage } from './pages/admin/AdminLoginPage'
import { PlanesPage } from './pages/admin/PlanesPage'
import { SuscripcionesPage } from './pages/admin/SuscripcionesPage'
import { FacturacionPage } from './pages/admin/FacturacionPage'
import { NegociosPage } from './pages/admin/NegociosPage'
import { NegocioDetailPage } from './pages/admin/NegocioDetailPage'

function App() {
  return (
    <Routes>
      <Route path="/admin/login" element={<AdminLoginPage />} />
      <Route path="/admin" element={<RequireAdminAuth />}>
        <Route element={<AdminLayout />}>
          <Route index element={<Navigate to="/admin/planes" replace />} />
          <Route path="planes" element={<PlanesPage />} />
          <Route path="suscripciones" element={<SuscripcionesPage />} />
          <Route path="facturacion" element={<FacturacionPage />} />
          <Route path="negocios" element={<NegociosPage />} />
          <Route path="negocios/:id" element={<NegocioDetailPage />} />
        </Route>
      </Route>

      <Route path="/panel/login" element={<LoginPage />} />
      <Route path="/panel/registro" element={<RegisterPage />} />
      <Route path="/panel/olvide-password" element={<OlvidePasswordPage />} />
      <Route path="/panel/restablecer-password" element={<RestablecerPasswordPage />} />
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
          <Route path="negocio" element={<MiNegocioPage />} />
          <Route path="perfil" element={<PerfilPage />} />
        </Route>
      </Route>

      <Route path="/" element={<LandingPage />} />
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
