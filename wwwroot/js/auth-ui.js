document.addEventListener("submit", (e) => {
    const form = e.target;
    if (form.id !== "loginForm" && form.id !== "registerForm") return;

    e.preventDefault();

    const data = Object.fromEntries(new FormData(form).entries());
    console.log("Form submit (frontend-only):", data);

    alert("Frontend-only: form captured. No backend yet.");
});
