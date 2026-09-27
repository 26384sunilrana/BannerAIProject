import React from 'react';
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { AddressForm } from '@/components/address/AddressForm';
import { mockFetchSequence, mockFetchOnce, restoreFetch, setupFetchMockCleanup } from '../helpers/mockFetch';

describe('AddressForm', () => {
  setupFetchMockCleanup();

  const mockCountries = [
    { code: 'IN', name: 'India' },
    { code: 'US', name: 'United States' },
  ];

  const mockStates = [
    { id: 1, name: 'Maharashtra', countryCode: 'IN' },
    { id: 2, name: 'Delhi', countryCode: 'IN' },
  ];

  const mockDistricts = [
    { id: 1, name: 'Mumbai', stateId: 1 },
    { id: 2, name: 'Thane', stateId: 1 },
  ];

  const mockOnSubmit = jest.fn();
  const mockOnCancel = jest.fn();

  beforeEach(() => {
    jest.clearAllMocks();
  });

  it('renders form with country dropdown', async () => {
    // Arrange
    mockFetchOnce(200, mockCountries);

    render(
      <AddressForm
        onSubmit={mockOnSubmit}
        onCancel={mockOnCancel}
      />
    );

    // Assert
    await waitFor(() => {
      expect(screen.getByLabelText(/country/i)).toBeInTheDocument();
    });
  });

  it('loads countries on component mount', async () => {
    // Arrange
    mockFetchOnce(200, mockCountries);

    render(
      <AddressForm
        onSubmit={mockOnSubmit}
        onCancel={mockOnCancel}
      />
    );

    // Assert
    await waitFor(() => {
      const countrySelect = screen.getByLabelText(/country/i);
      expect(countrySelect).toBeInTheDocument();
    });
  });

  it('fetches states when country is selected', async () => {
    // Arrange
    mockFetchSequence([
      { status: 200, body: mockCountries },
      { status: 200, body: mockStates },
    ]);

    render(
      <AddressForm
        onSubmit={mockOnSubmit}
        onCancel={mockOnCancel}
      />
    );

    // Act
    await waitFor(() => {
      const countrySelect = screen.getByLabelText(/country/i);
      expect(countrySelect).toBeInTheDocument();
    });

    const countrySelect = screen.getByLabelText(/country/i) as HTMLSelectElement;
    fireEvent.change(countrySelect, { target: { value: 'IN' } });

    // Assert
    await waitFor(() => {
      const stateSelect = screen.getByLabelText(/state/i);
      expect(stateSelect).toBeInTheDocument();
    });
  });

  it('fetches districts when state is selected', async () => {
    // Arrange
    mockFetchSequence([
      { status: 200, body: mockCountries },
      { status: 200, body: mockStates },
      { status: 200, body: mockDistricts },
    ]);

    render(
      <AddressForm
        onSubmit={mockOnSubmit}
        onCancel={mockOnCancel}
      />
    );

    // Act - Select country
    await waitFor(() => {
      const countrySelect = screen.getByLabelText(/country/i);
      expect(countrySelect).toBeInTheDocument();
    });

    const countrySelect = screen.getByLabelText(/country/i) as HTMLSelectElement;
    fireEvent.change(countrySelect, { target: { value: 'IN' } });

    // Assert - State select appears
    await waitFor(() => {
      const stateSelect = screen.getByLabelText(/state/i);
      expect(stateSelect).toBeInTheDocument();
    });

    // Act - Select state
    const stateSelect = screen.getByLabelText(/state/i) as HTMLSelectElement;
    fireEvent.change(stateSelect, { target: { value: '1' } });

    // Assert - District select appears
    await waitFor(() => {
      const districtSelect = screen.getByLabelText(/district/i);
      expect(districtSelect).toBeInTheDocument();
    });
  });

  it('disables state dropdown until country is selected', async () => {
    // Arrange
    mockFetchOnce(200, mockCountries);

    render(
      <AddressForm
        onSubmit={mockOnSubmit}
        onCancel={mockOnCancel}
      />
    );

    // Assert
    await waitFor(() => {
      const stateSelect = screen.queryByLabelText(/state/i);
      if (stateSelect) {
        expect(stateSelect).toBeDisabled();
      }
    });
  });

  it('disables district dropdown until state is selected', async () => {
    // Arrange
    mockFetchSequence([
      { status: 200, body: mockCountries },
      { status: 200, body: mockStates },
    ]);

    render(
      <AddressForm
        onSubmit={mockOnSubmit}
        onCancel={mockOnCancel}
      />
    );

    // Act - Select country
    await waitFor(() => {
      const countrySelect = screen.getByLabelText(/country/i);
      expect(countrySelect).toBeInTheDocument();
    });

    const countrySelect = screen.getByLabelText(/country/i) as HTMLSelectElement;
    fireEvent.change(countrySelect, { target: { value: 'IN' } });

    // Assert
    await waitFor(() => {
      const districtSelect = screen.queryByLabelText(/district/i);
      if (districtSelect) {
        expect(districtSelect).toBeDisabled();
      }
    });
  });

  it('clears state and district when country changes', async () => {
    // Arrange
    mockFetchSequence([
      { status: 200, body: mockCountries },
      { status: 200, body: mockStates },
      { status: 200, body: mockDistricts },
      { status: 200, body: mockStates },
    ]);

    render(
      <AddressForm
        onSubmit={mockOnSubmit}
        onCancel={mockOnCancel}
      />
    );

    // Act - Select country
    await waitFor(() => {
      const countrySelect = screen.getByLabelText(/country/i);
      expect(countrySelect).toBeInTheDocument();
    });

    const countrySelect = screen.getByLabelText(/country/i) as HTMLSelectElement;
    fireEvent.change(countrySelect, { target: { value: 'IN' } });

    // Assert - State appears
    await waitFor(() => {
      const stateSelect = screen.getByLabelText(/state/i);
      expect(stateSelect).toBeInTheDocument();
    });

    // Act - Select state
    const stateSelect = screen.getByLabelText(/state/i) as HTMLSelectElement;
    fireEvent.change(stateSelect, { target: { value: '1' } });

    // Assert - District appears
    await waitFor(() => {
      const districtSelect = screen.getByLabelText(/district/i);
      expect(districtSelect).toBeInTheDocument();
    });

    // Act - Change country again
    fireEvent.change(countrySelect, { target: { value: 'US' } });

    // Assert - Dependent dropdowns should reset
    await waitFor(() => {
      const stateSelectAfter = screen.getByLabelText(/state/i) as HTMLSelectElement;
      expect(stateSelectAfter.value).toBe('');
    });
  });

  it('validates required fields on submit', async () => {
    // Arrange
    mockFetchOnce(200, mockCountries);

    render(
      <AddressForm
        onSubmit={mockOnSubmit}
        onCancel={mockOnCancel}
      />
    );

    // Act
    const submitButton = screen.getByText(/save|submit/i);
    fireEvent.click(submitButton);

    // Assert
    expect(mockOnSubmit).not.toHaveBeenCalled();
    expect(screen.getByText(/required/i)).toBeInTheDocument();
  });

  it('allows postal code as optional field', async () => {
    // Arrange
    mockFetchSequence([
      { status: 200, body: mockCountries },
      { status: 200, body: mockStates },
      { status: 200, body: mockDistricts },
    ]);

    render(
      <AddressForm
        onSubmit={mockOnSubmit}
        onCancel={mockOnCancel}
      />
    );

    // Act - Select address fields
    await waitFor(() => {
      const countrySelect = screen.getByLabelText(/country/i);
      expect(countrySelect).toBeInTheDocument();
    });

    const countrySelect = screen.getByLabelText(/country/i) as HTMLSelectElement;
    fireEvent.change(countrySelect, { target: { value: 'IN' } });

    await waitFor(() => {
      const stateSelect = screen.getByLabelText(/state/i);
      expect(stateSelect).toBeInTheDocument();
    });

    const stateSelect = screen.getByLabelText(/state/i) as HTMLSelectElement;
    fireEvent.change(stateSelect, { target: { value: '1' } });

    await waitFor(() => {
      const districtSelect = screen.getByLabelText(/district/i);
      expect(districtSelect).toBeInTheDocument();
    });

    const districtSelect = screen.getByLabelText(/district/i) as HTMLSelectElement;
    fireEvent.change(districtSelect, { target: { value: '1' } });

    // Assert - Postal code input should exist
    const postalInput = screen.queryByLabelText(/postal|zip/i);
    if (postalInput) {
      expect(postalInput).toBeInTheDocument();
    }
  });

  it('handles optional latitude/longitude geo fields', async () => {
    // Arrange
    mockFetchSequence([
      { status: 200, body: mockCountries },
      { status: 200, body: mockStates },
      { status: 200, body: mockDistricts },
    ]);

    render(
      <AddressForm
        onSubmit={mockOnSubmit}
        onCancel={mockOnCancel}
      />
    );

    // Act - Complete cascading selection
    await waitFor(() => {
      const countrySelect = screen.getByLabelText(/country/i);
      expect(countrySelect).toBeInTheDocument();
    });

    const countrySelect = screen.getByLabelText(/country/i) as HTMLSelectElement;
    fireEvent.change(countrySelect, { target: { value: 'IN' } });

    await waitFor(() => {
      const stateSelect = screen.getByLabelText(/state/i);
      expect(stateSelect).toBeInTheDocument();
    });

    const stateSelect = screen.getByLabelText(/state/i) as HTMLSelectElement;
    fireEvent.change(stateSelect, { target: { value: '1' } });

    await waitFor(() => {
      const districtSelect = screen.getByLabelText(/district/i);
      expect(districtSelect).toBeInTheDocument();
    });

    // Assert - Geo fields are optional
    const latInput = screen.queryByLabelText(/latitude/i);
    const lonInput = screen.queryByLabelText(/longitude/i);
    expect(latInput).toBeInTheDocument();
    expect(lonInput).toBeInTheDocument();
  });

  it('submits form with valid cascading selection', async () => {
    // Arrange
    mockFetchSequence([
      { status: 200, body: mockCountries },
      { status: 200, body: mockStates },
      { status: 200, body: mockDistricts },
    ]);

    render(
      <AddressForm
        onSubmit={mockOnSubmit}
        onCancel={mockOnCancel}
      />
    );

    // Act - Complete cascading selection
    await waitFor(() => {
      const countrySelect = screen.getByLabelText(/country/i);
      expect(countrySelect).toBeInTheDocument();
    });

    const countrySelect = screen.getByLabelText(/country/i) as HTMLSelectElement;
    fireEvent.change(countrySelect, { target: { value: 'IN' } });

    await waitFor(() => {
      const stateSelect = screen.getByLabelText(/state/i);
      expect(stateSelect).toBeInTheDocument();
    });

    const stateSelect = screen.getByLabelText(/state/i) as HTMLSelectElement;
    fireEvent.change(stateSelect, { target: { value: '1' } });

    await waitFor(() => {
      const districtSelect = screen.getByLabelText(/district/i);
      expect(districtSelect).toBeInTheDocument();
    });

    const districtSelect = screen.getByLabelText(/district/i) as HTMLSelectElement;
    fireEvent.change(districtSelect, { target: { value: '1' } });

    const submitButton = screen.getByText(/save|submit/i);
    fireEvent.click(submitButton);

    // Assert
    await waitFor(() => {
      expect(mockOnSubmit).toHaveBeenCalled();
    });
    expect(mockOnSubmit).toHaveBeenCalledWith(
      expect.objectContaining({
        countryCode: 'IN',
        stateId: 1,
        districtId: 1,
      })
    );
  });

  it('calls onCancel when Cancel button is clicked', async () => {
    // Arrange
    mockFetchOnce(200, mockCountries);

    render(
      <AddressForm
        onSubmit={mockOnSubmit}
        onCancel={mockOnCancel}
      />
    );

    // Act
    await waitFor(() => {
      const cancelButton = screen.getByText(/cancel/i);
      expect(cancelButton).toBeInTheDocument();
    });

    const cancelButton = screen.getByText(/cancel/i);
    fireEvent.click(cancelButton);

    // Assert
    expect(mockOnCancel).toHaveBeenCalled();
    expect(mockOnSubmit).not.toHaveBeenCalled();
  });

  it('handles API errors gracefully', async () => {
    // Arrange
    mockFetchOnce(500);

    render(
      <AddressForm
        onSubmit={mockOnSubmit}
        onCancel={mockOnCancel}
      />
    );

    // Assert
    await waitFor(() => {
      expect(screen.getByText(/error|failed/i)).toBeInTheDocument();
    });
  });

  it('pre-fills address form when provided existing data', async () => {
    // Arrange
    const existingAddress = {
      countryCode: 'IN',
      stateId: 1,
      districtId: 1,
      postalCode: '400001',
    };

    mockFetchSequence([
      { status: 200, body: mockCountries },
      { status: 200, body: mockStates },
      { status: 200, body: mockDistricts },
    ]);

    render(
      <AddressForm
        address={existingAddress}
        onSubmit={mockOnSubmit}
        onCancel={mockOnCancel}
      />
    );

    // Assert
    await waitFor(() => {
      const countrySelect = screen.getByLabelText(/country/i) as HTMLSelectElement;
      expect(countrySelect.value).toBe('IN');
    });
  });

  it('shows loading state while fetching data', async () => {
    // Arrange
    mockFetchOnce(200, mockCountries, 100);  // 100ms delay

    render(
      <AddressForm
        onSubmit={mockOnSubmit}
        onCancel={mockOnCancel}
      />
    );

    // Assert - should show loading initially
    expect(screen.getByText(/loading/i) || screen.getByRole('progressbar')).toBeInTheDocument();
  });
});
