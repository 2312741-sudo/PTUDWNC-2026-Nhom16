'use client';

import React from 'react';

interface Option {
  value: string;
  label: string;
}

interface SearchFilterSelectProps {
  name: string;
  defaultValue: string;
  options: Option[];
}

export function SearchFilterSelect({ name, defaultValue, options }: SearchFilterSelectProps) {
  return (
    <select
      name={name}
      defaultValue={defaultValue}
      onChange={(e) => {
        const form = e.target.form;
        if (form) {
          form.requestSubmit();
        }
      }}
      className="py-1.5 px-2.5 bg-white border border-gray-200 rounded-xl text-xs font-medium text-gray-700 focus:outline-none focus:ring-1 focus:ring-emerald-500"
    >
      {options.map((opt) => (
        <option key={opt.value} value={opt.value}>
          {opt.label}
        </option>
      ))}
    </select>
  );
}
