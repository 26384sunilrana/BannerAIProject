import React from 'react'

export interface InputProps extends React.InputHTMLAttributes<HTMLInputElement> {
  label?: string
  error?: string
  helperText?: string
}

export const Input = React.forwardRef<HTMLInputElement, InputProps>(
  ({ label, error, helperText, className, ...props }, ref) => {
    return (
      <div className="flex flex-col gap-1 w-full">
        {label && (
          <label htmlFor={props.id} className="text-sm font-medium text-gray-700">
            {label}
          </label>
        )}
        <input
          ref={ref}
          className={`
            px-3 py-2 rounded-lg border-2
            font-base text-gray-900 placeholder-gray-400
            transition-colors duration-200
            ${error ? 'border-red-500 focus:border-red-600' : 'border-gray-300 focus:border-blue-500'}
            focus-visible:outline-none
            disabled:bg-gray-100 disabled:cursor-not-allowed disabled:opacity-60
            ${className || ''}
          `}
          {...props}
        />
        {error && <span className="text-sm text-red-600">{error}</span>}
        {helperText && !error && <span className="text-sm text-gray-500">{helperText}</span>}
      </div>
    )
  }
)

Input.displayName = 'Input'
