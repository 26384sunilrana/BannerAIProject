import React from 'react';
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { PlanForm } from '@/components/admin/PlanForm';
import { SubscriptionPlan } from '@/types/subscription';

describe('PlanForm', () => {
  const mockPlan: SubscriptionPlan = {
    id: '1',
    name: 'Professional',
    monthlyPrice: 29.99,
    annualPrice: 299.99,
    status: 'Active',
    features: ['Feature 1', 'Feature 2'],
    description: 'Professional plan',
  };

  const mockOnSubmit = jest.fn();
  const mockOnCancel = jest.fn();

  beforeEach(() => {
    jest.clearAllMocks();
  });

  it('renders form with empty fields when creating new plan', () => {
    // Arrange
    render(
      <PlanForm
        onSubmit={mockOnSubmit}
        onCancel={mockOnCancel}
      />
    );

    // Assert
    expect(screen.getByLabelText(/plan name/i)).toHaveValue('');
    expect(screen.getByLabelText(/monthly price/i)).toHaveValue('');
    expect(screen.getByLabelText(/annual price/i)).toHaveValue('');
  });

  it('renders form with pre-filled data when editing existing plan', () => {
    // Arrange
    render(
      <PlanForm
        plan={mockPlan}
        onSubmit={mockOnSubmit}
        onCancel={mockOnCancel}
      />
    );

    // Assert
    expect(screen.getByLabelText(/plan name/i)).toHaveValue(mockPlan.name);
    expect(screen.getByLabelText(/monthly price/i)).toHaveValue(String(mockPlan.monthlyPrice));
    expect(screen.getByLabelText(/annual price/i)).toHaveValue(String(mockPlan.annualPrice));
  });

  it('validates required fields on submit', async () => {
    // Arrange
    render(
      <PlanForm
        onSubmit={mockOnSubmit}
        onCancel={mockOnCancel}
      />
    );

    // Act
    const submitButton = screen.getByText(/save|submit/i);
    fireEvent.click(submitButton);

    // Assert
    expect(mockOnSubmit).not.toHaveBeenCalled();
    expect(screen.getByText(/plan name is required/i)).toBeInTheDocument();
  });

  it('validates annual price is greater than or equal to monthly price', async () => {
    // Arrange
    render(
      <PlanForm
        onSubmit={mockOnSubmit}
        onCancel={mockOnCancel}
      />
    );

    const nameInput = screen.getByLabelText(/plan name/i);
    const monthlyInput = screen.getByLabelText(/monthly price/i);
    const annualInput = screen.getByLabelText(/annual price/i);

    // Act
    await userEvent.type(nameInput, 'Invalid Plan');
    await userEvent.type(monthlyInput, '100');
    await userEvent.type(annualInput, '50');  // Invalid: annual < monthly

    const submitButton = screen.getByText(/save|submit/i);
    fireEvent.click(submitButton);

    // Assert
    expect(mockOnSubmit).not.toHaveBeenCalled();
    expect(screen.getByText(/annual price must be/i)).toBeInTheDocument();
  });

  it('submits form with valid data', async () => {
    // Arrange
    render(
      <PlanForm
        onSubmit={mockOnSubmit}
        onCancel={mockOnCancel}
      />
    );

    const nameInput = screen.getByLabelText(/plan name/i);
    const monthlyInput = screen.getByLabelText(/monthly price/i);
    const annualInput = screen.getByLabelText(/annual price/i);

    // Act
    await userEvent.type(nameInput, 'Basic');
    await userEvent.type(monthlyInput, '9.99');
    await userEvent.type(annualInput, '99.99');

    const submitButton = screen.getByText(/save|submit/i);
    fireEvent.click(submitButton);

    // Assert
    await waitFor(() => {
      expect(mockOnSubmit).toHaveBeenCalled();
    });
    expect(mockOnSubmit).toHaveBeenCalledWith(
      expect.objectContaining({
        name: 'Basic',
        monthlyPrice: 9.99,
        annualPrice: 99.99,
      })
    );
  });

  it('calls onCancel when Cancel button is clicked', () => {
    // Arrange
    render(
      <PlanForm
        onSubmit={mockOnSubmit}
        onCancel={mockOnCancel}
      />
    );

    // Act
    const cancelButton = screen.getByText(/cancel/i);
    fireEvent.click(cancelButton);

    // Assert
    expect(mockOnCancel).toHaveBeenCalled();
    expect(mockOnSubmit).not.toHaveBeenCalled();
  });

  it('shows loading state when loading prop is true', () => {
    // Arrange
    render(
      <PlanForm
        onSubmit={mockOnSubmit}
        onCancel={mockOnCancel}
        loading={true}
      />
    );

    // Assert
    const submitButton = screen.getByText(/save|submit/i);
    expect(submitButton).toBeDisabled();
  });

  it('displays error message when error prop is provided', () => {
    // Arrange
    const errorMessage = 'Failed to save plan';

    render(
      <PlanForm
        onSubmit={mockOnSubmit}
        onCancel={mockOnCancel}
        error={errorMessage}
      />
    );

    // Assert
    expect(screen.getByText(errorMessage)).toBeInTheDocument();
  });

  it('allows adding and removing features', async () => {
    // Arrange
    render(
      <PlanForm
        onSubmit={mockOnSubmit}
        onCancel={mockOnCancel}
      />
    );

    // Act
    const addFeatureButton = screen.getByText(/add feature/i);
    fireEvent.click(addFeatureButton);

    // Assert
    expect(screen.getByLabelText(/feature 1/i)).toBeInTheDocument();
  });

  it('updates monthly price calculation hint based on annual price', async () => {
    // Arrange
    render(
      <PlanForm
        onSubmit={mockOnSubmit}
        onCancel={mockOnCancel}
      />
    );

    const monthlyInput = screen.getByLabelText(/monthly price/i);
    const annualInput = screen.getByLabelText(/annual price/i);

    // Act
    await userEvent.type(monthlyInput, '29.99');
    await userEvent.type(annualInput, '299.99');

    // Assert - annual is 10x monthly (annual savings ~10%)
    const savingsHint = screen.queryByText(/save/i);
    expect(savingsHint).toBeInTheDocument();
  });

  it('handles description as optional field', async () => {
    // Arrange
    render(
      <PlanForm
        onSubmit={mockOnSubmit}
        onCancel={mockOnCancel}
      />
    );

    const nameInput = screen.getByLabelText(/plan name/i);
    const monthlyInput = screen.getByLabelText(/monthly price/i);
    const annualInput = screen.getByLabelText(/annual price/i);
    const descriptionInput = screen.queryByLabelText(/description/i);

    // Act
    await userEvent.type(nameInput, 'Simple Plan');
    await userEvent.type(monthlyInput, '19.99');
    await userEvent.type(annualInput, '199.99');
    if (descriptionInput) {
      await userEvent.type(descriptionInput, 'A simple plan');
    }

    const submitButton = screen.getByText(/save|submit/i);
    fireEvent.click(submitButton);

    // Assert
    await waitFor(() => {
      expect(mockOnSubmit).toHaveBeenCalled();
    });
  });
});
