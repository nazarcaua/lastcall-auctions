document.addEventListener("submit", async (e) => {
    const form = e.target;
    if (form.id !== "loginForm" && form.id !== "registerForm") return;

    e.preventDefault();

    const data = Object.fromEntries(new FormData(form).entries());

    let endpoint;
    let payload;

    if (form.id === "registerForm") {
        if (data.password !== data.confirmPassword) {
            alert("Passwords do not match.");
            return;
        }

        endpoint = "/api/users/register";
        payload = {
            email: data.email,
            password: data.password,
            fullName: data.fullName
        };
    } else {
        endpoint = "/api/users/login";
        payload = {
            email: data.email,
            password: data.password
        };
    }

    try {
        const response = await fetch(endpoint, {
            method: "POST",
            headers: {
                "Content-Type": "application/json"
            },
            body: JSON.stringify(payload)
        });

        const result = await response.json();

        if (!response.ok) {
            alert(result.message || "An error occurred.");
            return;
        }

        // Save token & user info
        localStorage.setItem("authToken", result.token);
        localStorage.setItem("authUser", JSON.stringify(result.user));

        // Redirect after success
        window.location.href = "/";
    } catch (error) {
        console.error("Auth error:", error);
        alert("Network or server error. Please try again.");
    }
});