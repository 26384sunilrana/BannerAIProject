import React from 'react'

export interface SelectOption {
  value: string | number
  label: string
}

export interface SelectProps extends React.SelectHTMLAttributes<HTMLSelectElement> {
  label?: string
  error?: string
  options: SelectOption[]
  helperText?: string
  /** Text of the empty choice at the top of the list. */
  placeholder?: string
}

export const Select = React.forwardRef<HTMLSelectElement, SelectProps>(
  ({ label, error, options, helperText, placeholder = 'Select an option', className, ...props }, ref) => {
    // a field without an id still needs its label tied to it, for screen readers and for clicking the label
    const generatedId = React.useId()
    const fieldId = props.id ?? generatedId
    return (
      <div className="flex flex-col gap-1 w-full">
        {label && (
          <label htmlFor={fieldId} className="text-sm font-medium text-gray-700">
            {label}
          </label>
        )}
        <select
          ref={ref}
          id={fieldId}
          className={`
            px-3 py-2 rounded-lg border-2
            font-base text-gray-900 bg-white
            transition-colors duration-200
            ${error ? 'border-red-500 focus:border-red-600' : 'border-gray-300 focus:border-blue-500'}
            focus-visible:outline-none
            disabled:bg-gray-100 disabled:cursor-not-allowed disabled:opacity-60
            ${className || ''}
          `}
          {...props}
        >
          <option value="">{placeholder}</option>
          {options.map((option) => (
            <option key={option.value} value={option.value}>
              {option.label}
            </option>
          ))}
        </select>
        {error && <span className="text-sm text-red-600">{error}</span>}
        {helperText && !error && <span className="text-sm text-gray-500">{helperText}</span>}
      </div>
    )
  }
)

Select.displayName = 'Select'
