/** @type {import('tailwindcss').Config} */
module.exports = {
  content: ["./src/**/*.{html,ts}"],
  theme: {
    extend: {
      colors: {
        forest: {
          DEFAULT: "#0F6B4C",
          dark: "#0A4D37",
          light: "#17835E"
        },
        gold: {
          DEFAULT: "#C9A227",
          dark: "#A6851C"
        },
        paper: "#F3EEE3",
        ink: "#1C1917"
      },
      fontFamily: {
        sans: ['"Source Sans 3"', "ui-sans-serif", "system-ui", "sans-serif"],
        display: ['Fraunces', "Georgia", "serif"]
      }
    }
  },
  plugins: []
};
