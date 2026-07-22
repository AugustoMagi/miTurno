export function FieldError({ message }: { message?: string }) {
  if (!message) return null
  return <span className="text-xs font-normal text-red-600">{message}</span>
}
