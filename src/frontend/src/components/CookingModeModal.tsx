'use client';

import { useState, useEffect, useRef } from 'react';
import {
  X,
  ChevronLeft,
  ChevronRight,
  Play,
  Pause,
  RotateCcw,
  Timer,
  CheckCircle2,
  Utensils,
  Volume2,
  Sparkles,
} from 'lucide-react';
import { RecipeStep, RecipeIngredient } from '@/types/recipe';

interface CookingModeModalProps {
  isOpen: boolean;
  onClose: () => void;
  title: string;
  steps: RecipeStep[];
  ingredients: RecipeIngredient[];
  servings: number;
}

export default function CookingModeModal({
  isOpen,
  onClose,
  title,
  steps,
  ingredients,
  servings,
}: CookingModeModalProps) {
  const [currentStepIndex, setCurrentStepIndex] = useState(0);
  const [completedSteps, setCompletedSteps] = useState<number[]>([]);
  const [isIngredientsOpen, setIsIngredientsOpen] = useState(false);
  const [showCelebration, setShowCelebration] = useState(false);

  // Timer states for current step
  const [timeLeft, setTimeLeft] = useState<number | null>(null);
  const [isTimerRunning, setIsTimerRunning] = useState(false);
  const timerRef = useRef<NodeJS.Timeout | null>(null);

  const currentStep = steps[currentStepIndex];

  // Initialize or reset timer when step changes
  useEffect(() => {
    if (currentStep?.timerMinutes && currentStep.timerMinutes > 0) {
      setTimeLeft(currentStep.timerMinutes * 60);
    } else {
      setTimeLeft(null);
    }
    setIsTimerRunning(false);
    if (timerRef.current) clearInterval(timerRef.current);
  }, [currentStepIndex, currentStep?.timerMinutes]);

  // Handle countdown
  useEffect(() => {
    if (isTimerRunning && timeLeft !== null && timeLeft > 0) {
      timerRef.current = setInterval(() => {
        setTimeLeft((prev) => {
          if (prev === null || prev <= 1) {
            clearInterval(timerRef.current!);
            setIsTimerRunning(false);
            playNotificationSound();
            return 0;
          }
          return prev - 1;
        });
      }, 1000);
    } else {
      if (timerRef.current) clearInterval(timerRef.current);
    }

    return () => {
      if (timerRef.current) clearInterval(timerRef.current);
    };
  }, [isTimerRunning, timeLeft]);

  // Play pleasant two-tone chime via Web Audio API
  const playNotificationSound = () => {
    try {
      const audioCtx = new (window.AudioContext || (window as any).webkitAudioContext)();
      const osc = audioCtx.createOscillator();
      const gain = audioCtx.createGain();

      osc.type = 'sine';
      osc.frequency.setValueAtTime(587.33, audioCtx.currentTime); // D5
      osc.frequency.exponentialRampToValueAtTime(880, audioCtx.currentTime + 0.2); // A5

      gain.gain.setValueAtTime(0.3, audioCtx.currentTime);
      gain.gain.exponentialRampToValueAtTime(0.01, audioCtx.currentTime + 0.8);

      osc.connect(gain);
      gain.connect(audioCtx.destination);

      osc.start();
      osc.stop(audioCtx.currentTime + 0.8);
    } catch (e) {
      console.log('Audio not supported or allowed', e);
    }
  };

  // Keyboard navigation: Left/Right arrow
  useEffect(() => {
    if (!isOpen) return;
    const handleKeyDown = (e: KeyboardEvent) => {
      if (e.key === 'Escape') onClose();
      if (e.key === 'ArrowRight' && currentStepIndex < steps.length - 1) {
        setCurrentStepIndex((prev) => prev + 1);
      }
      if (e.key === 'ArrowLeft' && currentStepIndex > 0) {
        setCurrentStepIndex((prev) => prev - 1);
      }
    };
    window.addEventListener('keydown', handleKeyDown);
    return () => window.removeEventListener('keydown', handleKeyDown);
  }, [isOpen, currentStepIndex, steps.length, onClose]);

  if (!isOpen) return null;

  const toggleStepCompleted = (index: number) => {
    setCompletedSteps((prev) =>
      prev.includes(index) ? prev.filter((i) => i !== index) : [...prev, index]
    );
  };

  const formatTimerDisplay = (seconds: number) => {
    const mins = Math.floor(seconds / 60);
    const secs = seconds % 60;
    return `${mins.toString().padStart(2, '0')}:${secs.toString().padStart(2, '0')}`;
  };

  const progressPercent = Math.round(((currentStepIndex + 1) / steps.length) * 100);

  return (
    <div
      role="dialog"
      aria-modal="true"
      aria-label="Chế độ nấu ăn từng bước"
      className="fixed inset-0 z-50 bg-neutral-900/95 backdrop-blur-md flex flex-col text-white"
    >
      {/* Header bar */}
      <header className="flex items-center justify-between px-6 py-4 border-b border-neutral-800 bg-neutral-900/80">
        <div className="flex items-center gap-3">
          <span className="px-2.5 py-1 rounded-full text-xs font-bold bg-amber-500/20 text-amber-400 border border-amber-500/30">
            Chế độ Nấu ăn
          </span>
          <h2 className="text-base sm:text-lg font-bold truncate max-w-xs sm:max-w-md text-neutral-200">
            {title}
          </h2>
        </div>

        <div className="flex items-center gap-3">
          <button
            onClick={() => setIsIngredientsOpen(!isIngredientsOpen)}
            className="flex items-center gap-1.5 px-3 py-1.5 rounded-lg text-xs font-semibold bg-neutral-800 hover:bg-neutral-700 text-neutral-300 transition-colors"
          >
            <Utensils className="w-3.5 h-3.5 text-amber-400" />
            <span className="hidden sm:inline">Nguyên liệu</span> ({ingredients.length})
          </button>
          <button
            onClick={onClose}
            className="p-2 rounded-lg bg-neutral-800 hover:bg-neutral-700 text-neutral-400 hover:text-white transition-colors"
            aria-label="Đóng chế độ nấu ăn"
          >
            <X className="w-5 h-5" />
          </button>
        </div>
      </header>

      {/* Progress Bar */}
      <div className="w-full bg-neutral-800 h-1.5">
        <div
          className="bg-amber-500 h-1.5 transition-all duration-300"
          style={{ width: `${progressPercent}%` }}
        />
      </div>

      {/* Main Content Area */}
      <div className="flex-1 flex overflow-hidden relative">
        {/* Step details */}
        <div className="flex-1 overflow-y-auto px-6 py-8 flex flex-col justify-between max-w-4xl mx-auto w-full">
          {showCelebration ? (
            <div className="flex-1 flex flex-col items-center justify-center text-center space-y-6 py-12 animate-in fade-in zoom-in-95 duration-300">
              <div className="w-24 h-24 rounded-full bg-amber-500/20 border-2 border-amber-500/40 flex items-center justify-center text-amber-400 shadow-xl shadow-amber-500/10">
                <Sparkles className="w-12 h-12" />
              </div>
              <div className="space-y-2">
                <h3 className="text-3xl font-extrabold text-white">Chúc mừng bạn đã hoàn thành!</h3>
                <p className="text-neutral-400 max-w-md mx-auto text-base">
                  Bạn đã thực hiện đầy đủ {steps.length} bước của món <strong>{title}</strong>. Chúc bạn và gia đình có một bữa ăn ngon miệng!
                </p>
              </div>
              <div className="flex items-center gap-4 pt-4">
                <button
                  onClick={() => {
                    setShowCelebration(false);
                    setCurrentStepIndex(0);
                  }}
                  className="px-5 py-2.5 rounded-xl bg-neutral-800 hover:bg-neutral-700 text-neutral-200 text-sm font-semibold transition-colors"
                >
                  Xem lại từ đầu
                </button>
                <button
                  onClick={onClose}
                  className="px-6 py-2.5 rounded-xl bg-amber-500 hover:bg-amber-600 text-neutral-950 text-sm font-bold shadow-lg shadow-amber-500/25 transition-all"
                >
                  Xong & Đóng
                </button>
              </div>
            </div>
          ) : (
            <div className="space-y-8 flex-1">
              {/* Step counter & status */}
              <div className="flex items-center justify-between">
                <span className="text-sm font-bold text-amber-400 tracking-wider uppercase">
                  Bước {currentStepIndex + 1} / {steps.length}
                </span>

                <button
                  onClick={() => toggleStepCompleted(currentStepIndex)}
                  className={`inline-flex items-center gap-2 px-3 py-1.5 rounded-lg text-xs font-semibold transition-colors border ${
                    completedSteps.includes(currentStepIndex)
                      ? 'bg-emerald-500/20 text-emerald-400 border-emerald-500/30'
                      : 'bg-neutral-800 text-neutral-400 border-neutral-700 hover:text-white'
                  }`}
                >
                  <CheckCircle2 className="w-4 h-4" />
                  <span>
                    {completedSteps.includes(currentStepIndex)
                      ? 'Đã xong bước này'
                      : 'Đánh dấu đã xong'}
                  </span>
                </button>
              </div>

              {/* Step Title & Instruction */}
              <div className="space-y-4">
                <h3 className="text-2xl sm:text-3xl font-extrabold text-white leading-tight">
                  {currentStep?.title}
                </h3>
                <p className="text-neutral-300 text-lg sm:text-xl leading-relaxed whitespace-pre-line font-light">
                  {currentStep?.description}
                </p>
              </div>

              {/* Timer Widget if step has timer */}
              {currentStep?.timerMinutes && currentStep.timerMinutes > 0 && timeLeft !== null && (
                <div className="bg-neutral-800/80 border border-neutral-700/60 rounded-2xl p-6 sm:p-8 flex flex-col sm:flex-row items-center justify-between gap-6 shadow-xl">
                  <div className="flex items-center gap-4">
                    <div className="w-14 h-14 rounded-2xl bg-amber-500/20 border border-amber-500/40 flex items-center justify-center text-amber-400">
                      <Timer className="w-7 h-7" />
                    </div>
                    <div>
                      <p className="text-xs uppercase tracking-wider text-neutral-400 font-semibold">
                        Thời gian bước này
                      </p>
                      <p
                        className={`text-4xl sm:text-5xl font-mono font-bold tracking-tight ${
                          timeLeft === 0 ? 'text-emerald-400 animate-pulse' : 'text-white'
                        }`}
                      >
                        {formatTimerDisplay(timeLeft)}
                      </p>
                    </div>
                  </div>

                  <div className="flex items-center gap-3">
                    <button
                      onClick={() => setIsTimerRunning(!isTimerRunning)}
                      className={`flex items-center gap-2 px-6 py-3 rounded-xl font-bold text-sm transition-all shadow-md ${
                        isTimerRunning
                          ? 'bg-amber-600 hover:bg-amber-700 text-white shadow-amber-600/30'
                          : 'bg-amber-500 hover:bg-amber-400 text-neutral-950 shadow-amber-500/20'
                      }`}
                    >
                      {isTimerRunning ? (
                        <>
                          <Pause className="w-4 h-4" /> Tạm dừng
                        </>
                      ) : (
                        <>
                          <Play className="w-4 h-4" /> Bắt đầu hẹn giờ
                        </>
                      )}
                    </button>
                    <button
                      onClick={() => {
                        setIsTimerRunning(false);
                        setTimeLeft(currentStep.timerMinutes! * 60);
                      }}
                      className="p-3 rounded-xl bg-neutral-700 hover:bg-neutral-600 text-neutral-300 transition-colors"
                      title="Đặt lại đồng hồ"
                    >
                      <RotateCcw className="w-4 h-4" />
                    </button>
                  </div>
                </div>
              )}
            </div>
          )}

          {/* Navigation Controls */}
          {!showCelebration && (
            <div className="pt-8 flex items-center justify-between border-t border-neutral-800 mt-6">
              <button
                onClick={() => setCurrentStepIndex((prev) => Math.max(0, prev - 1))}
                disabled={currentStepIndex === 0}
                className="inline-flex items-center gap-2 px-5 py-2.5 rounded-xl text-sm font-semibold text-neutral-300 bg-neutral-800 hover:bg-neutral-700 disabled:opacity-40 disabled:pointer-events-none transition-colors"
              >
                <ChevronLeft className="w-4 h-4" /> Bước trước
              </button>

              <div className="hidden sm:flex items-center gap-1.5">
                {steps.map((_, i) => (
                  <button
                    key={i}
                    onClick={() => setCurrentStepIndex(i)}
                    className={`w-3 h-3 rounded-full transition-all ${
                      i === currentStepIndex
                        ? 'bg-amber-500 scale-125'
                        : completedSteps.includes(i)
                        ? 'bg-emerald-500'
                        : 'bg-neutral-700 hover:bg-neutral-600'
                    }`}
                    title={`Chuyển đến bước ${i + 1}`}
                  />
                ))}
              </div>

              {currentStepIndex < steps.length - 1 ? (
                <button
                  onClick={() => {
                    if (!completedSteps.includes(currentStepIndex)) {
                      setCompletedSteps((prev) => [...prev, currentStepIndex]);
                    }
                    setCurrentStepIndex((prev) => prev + 1);
                  }}
                  className="inline-flex items-center gap-2 px-6 py-2.5 rounded-xl text-sm font-bold bg-amber-500 hover:bg-amber-400 text-neutral-950 shadow-lg shadow-amber-500/20 transition-all"
                >
                  Bước tiếp theo <ChevronRight className="w-4 h-4" />
                </button>
              ) : (
                <button
                  onClick={() => {
                    if (!completedSteps.includes(currentStepIndex)) {
                      setCompletedSteps((prev) => [...prev, currentStepIndex]);
                    }
                    setShowCelebration(true);
                  }}
                  className="inline-flex items-center gap-2 px-6 py-2.5 rounded-xl text-sm font-bold bg-emerald-500 hover:bg-emerald-400 text-neutral-950 shadow-lg shadow-emerald-500/20 transition-all"
                >
                  Hoàn thành nấu ăn <CheckCircle2 className="w-4 h-4" />
                </button>
              )}
            </div>
          )}
        </div>

        {/* Ingredients Side Drawer */}
        {isIngredientsOpen && (
          <aside className="w-80 sm:w-96 border-l border-neutral-800 bg-neutral-900 p-6 overflow-y-auto flex flex-col justify-between animate-in slide-in-from-right duration-200">
            <div className="space-y-4">
              <div className="flex items-center justify-between pb-3 border-b border-neutral-800">
                <h4 className="font-bold text-neutral-200 text-sm uppercase tracking-wider flex items-center gap-2">
                  <Utensils className="w-4 h-4 text-amber-400" />
                  Nguyên liệu ({servings} người)
                </h4>
                <button
                  onClick={() => setIsIngredientsOpen(false)}
                  className="text-neutral-400 hover:text-white"
                >
                  <X className="w-4 h-4" />
                </button>
              </div>

              <ul className="space-y-2.5 text-sm">
                {ingredients.map((ing) => (
                  <li
                    key={ing.id}
                    className="p-3 rounded-xl bg-neutral-800/60 border border-neutral-750 flex items-start justify-between gap-3"
                  >
                    <span className="text-neutral-200 font-medium">{ing.name}</span>
                    <span className="text-amber-400 font-semibold shrink-0">
                      {ing.quantity != null ? `${ing.quantity} ` : ''}
                      {ing.unit ?? ''}
                    </span>
                  </li>
                ))}
              </ul>
            </div>

            <p className="text-xs text-neutral-500 text-center pt-4">
              Nhấn phím mũi tên Trái / Phải để chuyển bước nhanh
            </p>
          </aside>
        )}
      </div>
    </div>
  );
}
