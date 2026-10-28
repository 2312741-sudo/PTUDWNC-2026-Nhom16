import React from 'react';
import { render, screen, fireEvent } from '@testing-library/react';
import '@testing-library/jest-dom';
import FavoriteButton from '../FavoriteButton';

describe('FavoriteButton Component', () => {
  beforeEach(() => {
    localStorage.clear();
  });

  const recipe = {
    slug: 'cha-ca-la-vong',
    title: 'Chả cá Lã Vọng',
    categoryName: 'Món chiên',
  };

  it('renders icon variant with proper accessible label', () => {
    render(<FavoriteButton recipe={recipe} variant="icon" />);
    const btn = screen.getByRole('button', { name: /Lưu vào danh sách yêu thích/i });
    expect(btn).toBeInTheDocument();
    expect(btn).toHaveAttribute('aria-pressed', 'false');
  });

  it('toggles favorite state and updates aria-pressed and label on click', () => {
    render(<FavoriteButton recipe={recipe} variant="badge" />);
    const btn = screen.getByRole('button');
    expect(btn).toHaveTextContent('Lưu công thức');
    expect(btn).toHaveAttribute('aria-pressed', 'false');

    // Click to favorite
    fireEvent.click(btn);
    expect(btn).toHaveTextContent('Đã lưu yêu thích');
    expect(btn).toHaveAttribute('aria-pressed', 'true');

    // Click again to unfavorite
    fireEvent.click(btn);
    expect(btn).toHaveTextContent('Lưu công thức');
    expect(btn).toHaveAttribute('aria-pressed', 'false');
  });

  it('prevents event propagation when clicked inside a card', () => {
    const parentClick = jest.fn();
    render(
      <div onClick={parentClick}>
        <FavoriteButton recipe={recipe} variant="icon" />
      </div>
    );
    const btn = screen.getByRole('button');
    fireEvent.click(btn);
    expect(parentClick).not.toHaveBeenCalled();
  });
});
