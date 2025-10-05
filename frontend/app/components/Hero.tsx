import { ShortCodeInput } from '~/components/ShortCodeInput';

export function Hero() {
  return (
    <section className="bg-gradient-to-b from-purple-50 to-white py-16 md:py-24">
      <div className="container mx-auto px-4 flex flex-col items-center text-center">
        <h1 className="text-4xl md:text-6xl font-bold text-purple-900 mb-6">
          Kappi.gg
        </h1>
        <p className="text-xl text-gray-600 max-w-3xl mb-10">
          Share photos and videos in <b>original quality</b> without compression
        </p>

        <div className="w-full max-w-md mb-10">
          <a
            href="/upload"
            className="block w-full px-8 py-4 bg-purple-600 text-white text-lg font-bold rounded-lg hover:bg-purple-700 transition-colors"
          >
            Start Sharing
          </a>
        </div>

        <ShortCodeInput />
      </div>
    </section>
  );
}

