import Link from 'next/link'

export default function Home() {
  return (
    <div className="flex items-center justify-center min-h-screen bg-gray-50">
      <div className="text-center">
        <h1 className="text-4xl font-bold text-gray-900 mb-4">Banner Editor</h1>
        <p className="text-xl text-gray-600 mb-8">Create and edit banners with drag-and-drop</p>

        <div className="space-y-4">
          <p className="text-gray-500 mb-6">
            Enter a banner ID to start editing
          </p>

          <form className="max-w-sm mx-auto">
            <div className="flex gap-2">
              <input
                type="text"
                id="bannerId"
                placeholder="Banner ID"
                className="flex-1 px-4 py-2 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500"
              />
              <button
                type="submit"
                className="px-6 py-2 bg-blue-600 text-white rounded-lg hover:bg-blue-700 transition"
              >
                Open
              </button>
            </div>
          </form>

          <div className="mt-8 text-sm text-gray-500">
            <p>Example: Try ID: <code className="bg-gray-100 px-2 py-1 rounded">1</code></p>
          </div>
        </div>
      </div>
    </div>
  )
}
