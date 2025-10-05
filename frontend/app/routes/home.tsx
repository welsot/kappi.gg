import type { Route } from './+types/home';
import { Hero } from '~/components/Hero';
import { Footer } from '~/components/Footer';
import { InlineNavbar } from '~/components/InlineNavbar';

export function meta({}: Route.MetaArgs) {
  const baseUrl = "https://kappi.gg";
  return [
    { title: "Kappi.gg - Share Photos & Videos in Original Quality" },
    { name: "description", content: "Upload and share photos and videos in original quality. Easy sharing for travelers and photographers." },
    { name: "viewport", content: "width=device-width,initial-scale=1" },

    // Open Graph / Facebook
    { property: "og:type", content: "website" },
    { property: "og:url", content: baseUrl },
    { property: "og:title", content: "Kappi.gg - Photo & Video Sharing Platform" },
    { property: "og:description", content: "Share photos and videos in original quality without compression. Perfect for travelers and photographers." },
    { property: "og:image", content: `${baseUrl}/kappigg-screenshot.png` },

    // Twitter
    { name: "twitter:card", content: "summary_large_image" },
    { name: "twitter:url", content: baseUrl },
    { name: "twitter:title", content: "Kappi.gg - Photo & Video Sharing Platform" },
    { name: "twitter:description", content: "Share photos and videos in original quality without compression. Perfect for travelers and photographers." },
    { name: "twitter:image", content: `${baseUrl}/kappigg-screenshot.png` },

    // Icons
    { rel: "icon", href: "/img/icon.64.png", type: "image/png" },
    { rel: "apple-touch-icon", href: "/img/icon.256.png" },
    { rel: "manifest", href: "/manifest.json" },

    // Color theme
    { name: "theme-color", content: "#59168b" }
  ];
}

export default function Home() {
  return (
    <div className="flex flex-col min-h-screen">
      <main className="flex-grow">
        <Hero />
      </main>
      <Footer />
    </div>
  );
}
