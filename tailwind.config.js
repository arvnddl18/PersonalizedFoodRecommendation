/** @type {import('tailwindcss').Config} */
module.exports = {
    content: [
        './Views/**/*.cshtml',
        './Pages/**/*.cshtml',
        './wwwroot/**/*.html',
        './Views/Shared/**/*.cshtml',
        './Views/Home/**/*.cshtml',
        './Views/Account/**/*.cshtml'
    ],
    theme: {
        extend: {
            colors: {
                // You can add your custom colors here
            },
            fontFamily: {
                // You can add your custom fonts here
            }
        },
    },
    plugins: [],
    darkMode: 'class'
}
