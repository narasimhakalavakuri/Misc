import axios from 'axios';
import { useState, useEffect } from 'react';
import { Link } from 'react-router-dom';
import './Home.css';

function Home() {
    const [user, setUser] = useState(null);

    // Check if we are already logged in via cookie
    useEffect(() => {
        axios.get('https://localhost:5001/auth/me', { withCredentials: true })
            .then(res => setUser(res.data.user))
            .catch(() => setUser(null));
    }, []);

    const login = () => {
        // This triggers the .NET "Challenge" which redirects to Entra ID
        window.location.href = "https://localhost:5001/auth/login";
    };

    return (
        <div className="home-container">
            <div className="home-content">
                {user ? (
                    <>
                        <h1 className="welcome-title">Welcome, {user}!</h1>
                        <p className="welcome-subtitle">You're successfully authenticated</p>
                        <Link to="/dashboard" className="dashboard-link">
                            Go to Dashboard →
                        </Link>
                    </>
                ) : (
                    <>
                        <h1 className="welcome-title">Welcome</h1>
                        <p className="welcome-subtitle">Please sign in to continue</p>
                        <button onClick={login} className="login-button">
                            Login with Entra ID
                        </button>
                    </>
                )}
            </div>
        </div>
    );
}

export default Home;
