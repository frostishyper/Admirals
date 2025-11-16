package com.admirals.lwjgl3;

import com.badlogic.gdx.backends.lwjgl3.Lwjgl3Application;
import com.badlogic.gdx.backends.lwjgl3.Lwjgl3ApplicationConfiguration;
import com.admirals.Center;

/** Launches the desktop (LWJGL3) application. */
public class Lwjgl3Launcher {
    public static void main(String[] args) {
        if (StartupHelper.startNewJvmIfRequired()) return; // Handles macOS support
        createApplication();
    }

    private static Lwjgl3Application createApplication() {
        return new Lwjgl3Application(new Center(), getDefaultConfiguration());
    }

    private static Lwjgl3ApplicationConfiguration getDefaultConfiguration() {
        Lwjgl3ApplicationConfiguration configuration = new Lwjgl3ApplicationConfiguration();
        configuration.setTitle("Admirals");

        // Vsync limits FPS to the monitor's refresh rate.
        configuration.useVsync(true);
        // Safeguard for fractional refresh rates.
        configuration.setForegroundFPS(Lwjgl3ApplicationConfiguration.getDisplayMode().refreshRate + 1);

        // Start in a maximized window with decorations (title bar, etc.)
        configuration.setWindowedMode(1280, 720); // Set a default size
        configuration.setMaximized(true);         // Request maximization

        // Set window icons (files in lwjgl3/src/main/resources/)
        configuration.setWindowIcon("libgdx128.png", "libgdx64.png", "libgdx32.png", "libgdx16.png");

        // Use ANGLE for OpenGL ES 2.0 emulation (improves compatibility)
        // Requires the gdx-lwjgl3-angle dependency.
        configuration.setOpenGLEmulation(Lwjgl3ApplicationConfiguration.GLEmulation.ANGLE_GLES20, 0, 0);

        return configuration;
    }
}
