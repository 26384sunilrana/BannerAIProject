import React from 'react'

export interface ButtonProps extends React.ButtonHTMLAttributes<HTMLButtonElement> {
  variant?: 'primary' | 'secondary' | 'danger' | 'ghost'
  size?: 'sm' | 'md' | 'lg'
  isLoading?: boolean
  children: React.ReactNode
}

const variantStyles = {
  primary: 'bg-blue-600 text-white hover:bg-blue-700 active:bg-blue-800',
  secondary: 'bg-gray-200 text-gray-900 hover:bg-gray-300 active:bg-gray-400',
  danger: 'bg-red-600 text-white hover:bg-red-700 active:bg-red-800',
  ghost: 'text-gray-700 hover:bg-gray-100 active:bg-gray-200',
}

const sizeStyles = {
  sm: 'px-3 py-1 text-sm',
  md: 'px-4 py-2 text-base',
  lg: 'px-6 py-3 text-lg',
}

export const Button = React.forwardRef<HTMLButtonElement, ButtonProps>(
  (
    {
      variant = 'primary',
      size = 'md',
      isLoading = false,
      disabled = false,
      children,
      className,
      ...props
    },
    ref
  ) => {
    return (
      <button
        ref={ref}
        disabled={disabled || isLoading}
        className={`
          inline-flex items-center justify-center gap-2
          rounded-lg font-medium transition-colors
          disabled:opacity-50 disabled:cursor-not-allowed
          focus-visible:outline-2 focus-visible:outline-blue-500 focus-visible:outline-offset-2
          ${variantStyles[variant]}
          ${sizeStyles[size]}
          ${className || ''}
        `}
        {...props}
      >
        {isLoading && (
          <span className="inline-block w-4 h-4 border-2 border-current border-t-transparent rounded-full animate-spin" />
        )}
        {children}
      </button>
    )
  }
)

Button.displayName = 'Button'
