import React from 'react';
import { render, screen, fireEvent } from '@testing-library/react';
import '@testing-library/jest-dom';
import CookingModeModal from '../CookingModeModal';
import { RecipeStep, RecipeIngredient } from '@/types/recipe';

describe('CookingModeModal Component', () => {
  const steps: RecipeStep[] = [
    {
      id: 'step-1',
      stepNumber: 1,
      title: 'Sơ chế nguyên liệu',
      description: 'Rửa sạch thịt bò và thái lát mỏng.',
      timerMinutes: null,
    },
    {
      id: 'step-2',
      stepNumber: 2,
      title: 'Nấu nước dùng',
      description: 'Hầm xương ống trong 30 phút cùng gia vị.',
      timerMinutes: 30,
    },
  ];

  const ingredients: RecipeIngredient[] = [
    { id: 'ing-1', name: 'Thịt bò', quantity: 500, unit: 'g', notes: null },
    { id: 'ing-2', name: 'Hành hoa', quantity: 50, unit: 'g', notes: null },
  ];

  it('does not render when isOpen is false', () => {
    const { container } = render(
      <CookingModeModal
        isOpen={false}
        onClose={jest.fn()}
        title="Phở bò"
        steps={steps}
        ingredients={ingredients}
        servings={4}
      />
    );
    expect(container.firstChild).toBeNull();
  });

  it('renders step 1 and navigates to step 2 when clicking next', () => {
    const onClose = jest.fn();
    render(
      <CookingModeModal
        isOpen={true}
        onClose={onClose}
        title="Phở bò"
        steps={steps}
        ingredients={ingredients}
        servings={4}
      />
    );

    expect(screen.getByText('Bước 1 / 2')).toBeInTheDocument();
    expect(screen.getByText('Sơ chế nguyên liệu')).toBeInTheDocument();
    expect(screen.getByText('Rửa sạch thịt bò và thái lát mỏng.')).toBeInTheDocument();

    // Click next step
    const nextBtn = screen.getByRole('button', { name: /Bước tiếp theo/i });
    fireEvent.click(nextBtn);

    expect(screen.getByText('Bước 2 / 2')).toBeInTheDocument();
    expect(screen.getByText('Nấu nước dùng')).toBeInTheDocument();
    expect(screen.getByText('30:00')).toBeInTheDocument(); // 30 mins timer formatted
  });

  it('toggles ingredient drawer', () => {
    render(
      <CookingModeModal
        isOpen={true}
        onClose={jest.fn()}
        title="Phở bò"
        steps={steps}
        ingredients={ingredients}
        servings={4}
      />
    );

    const ingBtn = screen.getByRole('button', { name: /Nguyên liệu/i });
    fireEvent.click(ingBtn);

    expect(screen.getByText('Thịt bò')).toBeInTheDocument();
    expect(screen.getByText('500 g')).toBeInTheDocument();
  });

  it('calls onClose when close button is clicked', () => {
    const onClose = jest.fn();
    render(
      <CookingModeModal
        isOpen={true}
        onClose={onClose}
        title="Phở bò"
        steps={steps}
        ingredients={ingredients}
        servings={4}
      />
    );

    const closeBtn = screen.getByLabelText('Đóng chế độ nấu ăn');
    fireEvent.click(closeBtn);
    expect(onClose).toHaveBeenCalledTimes(1);
  });
});
