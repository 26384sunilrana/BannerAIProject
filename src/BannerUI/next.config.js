/** @type {import('next').NextConfig} */
const nextConfig = {
  reactStrictMode: true,
  swcMinify: true,
  images: {
    unoptimized: process.env.NODE_ENV === 'development',
  },
  webpack: (config) => {
    config.module.rules.push({
      test: /\.module\.css$/,
      use: ['style-loader', 'css-loader?modules'],
    })
    return config
  },
}

module.exports = nextConfig
