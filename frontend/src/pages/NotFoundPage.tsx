export function NotFoundPage() {
  return (
    <div className="animate-fade-in-up mx-auto max-w-md py-16 text-center">
      <span className="mx-auto flex h-14 w-14 items-center justify-center rounded-full border-2 border-dashed border-slate-300 text-slate-400">
        <svg viewBox="0 0 24 24" fill="none" strokeWidth={1.8} stroke="currentColor" className="h-7 w-7">
          <path
            strokeLinecap="round"
            strokeLinejoin="round"
            d="M9.879 9.879a3 3 0 1 0 4.242 4.242M3 12a9 9 0 1 0 18 0 9 9 0 0 0-18 0Z"
          />
        </svg>
      </span>
      <h1 className="mt-4 text-xl font-semibold text-slate-900">Ingresá desde el link de tu negocio</h1>
      <p className="mt-2 text-sm text-slate-500">
        MiTurno no tiene una página de inicio pública: accedé con el enlace que te compartió el
        negocio (por ejemplo, desde su perfil de Instagram) para ver sus turnos disponibles.
      </p>
    </div>
  )
}
