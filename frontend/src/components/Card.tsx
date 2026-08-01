import type { HTMLAttributes } from 'react'

interface CardProps extends HTMLAttributes<HTMLDivElement> {
  hover?: boolean
}

export function Card({ className = '', hover = false, ...props }: CardProps) {
  return (
    <div
      className={`rounded-xl border border-slate-200 bg-white p-6 shadow-soft transition-all duration-200 ${
        hover ? 'hover:-translate-y-0.5 hover:shadow-soft-lg' : ''
      } ${className}`}
      {...props}
    />
  )
}
