import type { ButtonHTMLAttributes, ReactNode } from 'react'

type Variant = 'primary' | 'secondary' | 'ghost' | 'danger'
type Size = 'sm' | 'md'

interface ButtonProps extends ButtonHTMLAttributes<HTMLButtonElement> {
  variant?: Variant
  size?: Size
  loading?: boolean
  icon?: ReactNode
}

const variantClasses: Record<Variant, string> = {
  primary:
    'bg-accent-500 text-white shadow-soft hover:bg-accent-600 hover:shadow-soft-lg active:bg-accent-700 disabled:bg-slate-200 disabled:text-slate-400 disabled:shadow-none',
  secondary:
    'border border-slate-300 bg-white text-slate-700 shadow-soft hover:border-slate-400 hover:bg-slate-50 active:bg-slate-100 disabled:border-slate-200 disabled:text-slate-400 disabled:shadow-none',
  ghost:
    'text-slate-600 hover:bg-slate-100 hover:text-slate-900 active:bg-slate-200 disabled:text-slate-300',
  danger:
    'bg-red-600 text-white shadow-soft hover:bg-red-700 active:bg-red-800 disabled:bg-slate-200 disabled:text-slate-400 disabled:shadow-none',
}

const sizeClasses: Record<Size, string> = {
  sm: 'h-9 gap-1.5 px-3 text-sm',
  md: 'h-11 gap-2 px-5 text-sm',
}

export function Button({
  variant = 'primary',
  size = 'md',
  loading = false,
  icon,
  disabled,
  className = '',
  children,
  ...props
}: ButtonProps) {
  return (
    <button
      disabled={disabled || loading}
      className={`inline-flex select-none items-center justify-center rounded-xl font-medium tracking-tight transition-all duration-200 ease-out disabled:cursor-not-allowed disabled:transform-none ${sizeClasses[size]} ${variantClasses[variant]} ${className}`}
      {...props}
    >
      {loading ? (
        <>
          <span className="h-4 w-4 animate-spin rounded-full border-2 border-current border-t-transparent opacity-80" />
          <span>{children}</span>
        </>
      ) : (
        <>
          {icon && <span className="shrink-0 [&>svg]:h-4 [&>svg]:w-4">{icon}</span>}
          {children}
        </>
      )}
    </button>
  )
}
