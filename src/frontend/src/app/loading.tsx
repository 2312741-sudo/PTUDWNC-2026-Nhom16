import Loading from '@/components/Loading';

export default function RootLoading() {
  return (
    <div className="min-h-[calc(100vh-8rem)] flex items-center justify-center">
      <Loading message="Đang tải trang Culinary Blog..." size="lg" />
    </div>
  );
}
