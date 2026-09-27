import React from 'react';
import { render, screen, fireEvent } from '@testing-library/react';
import { ShopCard } from '@/components/shops/ShopCard';
import { ShopDto } from '@/types/shop';

describe('ShopCard', () => {
  const mockShop: ShopDto = {
    id: '1',
    name: 'Mumbai Central',
    description: 'Flagship store in Mumbai',
    city: 'Mumbai',
    address: '123 MG Road',
    stateName: 'Maharashtra',
    countryName: 'India',
    status: 'Active',
    phoneNumber: '+91-22-1234-5678',
    website: 'https://example.com',
    childShopsCount: 2,
    createdAt: new Date().toISOString(),
    updatedAt: new Date().toISOString(),
    countryCode: 'IN',
    stateId: 1,
    districtId: 1,
    postalCode: '400001',
  };

  const mockCallbacks = {
    onEdit: jest.fn(),
    onDeactivate: jest.fn(),
  };

  beforeEach(() => {
    jest.clearAllMocks();
  });

  it('renders shop card with all information', () => {
    // Arrange
    render(
      <ShopCard
        shop={mockShop}
        onEdit={mockCallbacks.onEdit}
        onDeactivate={mockCallbacks.onDeactivate}
      />
    );

    // Assert
    expect(screen.getByText('Mumbai Central')).toBeInTheDocument();
    expect(screen.getByText('Flagship store in Mumbai')).toBeInTheDocument();
    expect(screen.getByText(/Mumbai, Maharashtra, India/i)).toBeInTheDocument();
    expect(screen.getByText('+91-22-1234-5678')).toBeInTheDocument();
  });

  it('displays status badge correctly for Active status', () => {
    // Arrange
    render(
      <ShopCard
        shop={mockShop}
        onEdit={mockCallbacks.onEdit}
        onDeactivate={mockCallbacks.onDeactivate}
      />
    );

    // Assert
    const statusBadge = screen.getByText('Active');
    expect(statusBadge).toHaveClass('bg-green-100');
  });

  it('displays status badge correctly for Inactive status', () => {
    // Arrange
    const inactiveShop = { ...mockShop, status: 'Inactive' as const };

    render(
      <ShopCard
        shop={inactiveShop}
        onEdit={mockCallbacks.onEdit}
        onDeactivate={mockCallbacks.onDeactivate}
      />
    );

    // Assert
    const statusBadge = screen.getByText('Inactive');
    expect(statusBadge).toHaveClass('bg-yellow-100');
  });

  it('shows website as clickable link', () => {
    // Arrange
    render(
      <ShopCard
        shop={mockShop}
        onEdit={mockCallbacks.onEdit}
        onDeactivate={mockCallbacks.onDeactivate}
      />
    );

    // Assert
    const link = screen.getByText(mockShop.website!);
    expect(link).toHaveAttribute('href', mockShop.website);
    expect(link).toHaveAttribute('target', '_blank');
  });

  it('displays sub-shops count when greater than 0', () => {
    // Arrange
    render(
      <ShopCard
        shop={mockShop}
        onEdit={mockCallbacks.onEdit}
        onDeactivate={mockCallbacks.onDeactivate}
      />
    );

    // Assert
    expect(screen.getByText(/2 sub-shops/i)).toBeInTheDocument();
  });

  it('does not display sub-shops section when count is 0', () => {
    // Arrange
    const shopWithoutSubShops = { ...mockShop, childShopsCount: 0 };

    render(
      <ShopCard
        shop={shopWithoutSubShops}
        onEdit={mockCallbacks.onEdit}
        onDeactivate={mockCallbacks.onDeactivate}
      />
    );

    // Assert
    expect(screen.queryByText(/sub-shop/i)).not.toBeInTheDocument();
  });

  it('calls onEdit callback when Edit button is clicked', () => {
    // Arrange
    render(
      <ShopCard
        shop={mockShop}
        onEdit={mockCallbacks.onEdit}
        onDeactivate={mockCallbacks.onDeactivate}
      />
    );

    // Act
    const editButton = screen.getByText('Edit');
    fireEvent.click(editButton);

    // Assert
    expect(mockCallbacks.onEdit).toHaveBeenCalledWith(mockShop);
    expect(mockCallbacks.onEdit).toHaveBeenCalledTimes(1);
  });

  it('calls onDeactivate callback when Deactivate button is clicked', () => {
    // Arrange
    render(
      <ShopCard
        shop={mockShop}
        onEdit={mockCallbacks.onEdit}
        onDeactivate={mockCallbacks.onDeactivate}
      />
    );

    // Act
    const deactivateButton = screen.getByText('Deactivate');
    fireEvent.click(deactivateButton);

    // Assert
    expect(mockCallbacks.onDeactivate).toHaveBeenCalledWith(mockShop);
    expect(mockCallbacks.onDeactivate).toHaveBeenCalledTimes(1);
  });

  it('does not show Deactivate button when status is Inactive', () => {
    // Arrange
    const inactiveShop = { ...mockShop, status: 'Inactive' as const };

    render(
      <ShopCard
        shop={inactiveShop}
        onEdit={mockCallbacks.onEdit}
        onDeactivate={mockCallbacks.onDeactivate}
      />
    );

    // Assert
    expect(screen.queryByText('Deactivate')).not.toBeInTheDocument();
  });

  it('displays View button linking to shop detail', () => {
    // Arrange
    render(
      <ShopCard
        shop={mockShop}
        onEdit={mockCallbacks.onEdit}
        onDeactivate={mockCallbacks.onDeactivate}
      />
    );

    // Assert
    const viewButton = screen.getByText('View');
    expect(viewButton).toHaveAttribute('href', `/shops/${mockShop.id}`);
  });

  it('does not display optional fields when missing', () => {
    // Arrange
    const shopWithoutOptionals: ShopDto = {
      ...mockShop,
      website: undefined,
      phoneNumber: undefined,
      description: undefined,
    };

    render(
      <ShopCard
        shop={shopWithoutOptionals}
        onEdit={mockCallbacks.onEdit}
        onDeactivate={mockCallbacks.onDeactivate}
      />
    );

    // Assert
    expect(screen.queryByText(mockShop.website!)).not.toBeInTheDocument();
    expect(screen.queryByText(mockShop.phoneNumber!)).not.toBeInTheDocument();
  });

  it('shows all buttons: View, Edit, and Deactivate for Active shops', () => {
    // Arrange
    render(
      <ShopCard
        shop={mockShop}
        onEdit={mockCallbacks.onEdit}
        onDeactivate={mockCallbacks.onDeactivate}
      />
    );

    // Assert
    expect(screen.getByText('View')).toBeInTheDocument();
    expect(screen.getByText('Edit')).toBeInTheDocument();
    expect(screen.getByText('Deactivate')).toBeInTheDocument();
  });
});
