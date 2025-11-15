package com.admirals.utils;

import com.badlogic.gdx.graphics.OrthographicCamera;
import com.badlogic.gdx.utils.viewport.FitViewport;
import com.badlogic.gdx.utils.viewport.Viewport;

/**
 * Manages viewport and camera for consistent 1080p scaling.
 */
public class Scaling_Util {

    // Base resolution
    public static final float VIRTUAL_WIDTH = 1920f;
    public static final float VIRTUAL_HEIGHT = 1080f;

    private final Viewport viewport;
    private final OrthographicCamera camera;

    /**
     * Creates a new scaling utility.
     */
    public Scaling_Util() {
        camera = new OrthographicCamera();
        // FitViewport maintains 16:9 ratio.
        viewport = new FitViewport(VIRTUAL_WIDTH, VIRTUAL_HEIGHT, camera);
        // Center camera on virtual screen
        camera.position.set(VIRTUAL_WIDTH / 2f, VIRTUAL_HEIGHT / 2f, 0);
        viewport.apply();
        camera.update();
    }

    /**
     * Updates the viewport on screen resize.
     * Call in screen's resize().
     */
    public void update(int screenWidth, int screenHeight) {
        viewport.update(screenWidth, screenHeight, false); // Center camera
    }

    /**
     * Applies the viewport.
     * Call in screen's render().
     */
    public void apply() {
        viewport.apply();
    }

    // --- Getters ---

    public Viewport getViewport() {
        return viewport;
    }

    public OrthographicCamera getCamera() {
        return camera;
    }

    // --- Scaler Methods ---

    /**
     * Gets a width relative to the virtual screen.
     * @param percent (0.0 to 1.0) of virtual width.
     * @return The calculated width.
     */
    public float getWidth(float percent) {
        return VIRTUAL_WIDTH * percent;
    }

    /**
     * Gets a height relative to the virtual screen.
     * @param percent (0.0 to 1.0) of virtual height.
     * @return The calculated height.
     */
    public float getHeight(float percent) {
        return VIRTUAL_HEIGHT * percent;
    }

    /**
     * Gets an X position relative to the virtual screen.
     * @param percent (0.0 to 1.0) of virtual width.
     * @return The calculated X coordinate.
     */
    public float getX(float percent) {
        return VIRTUAL_WIDTH * percent;
    }

    /**
     * Gets a Y position relative to the virtual screen.
     * @param percent (0.0 to 1.0) of virtual height.
     * @return The calculated Y coordinate.
     */
    public float getY(float percent) {
        return VIRTUAL_HEIGHT * percent;
    }

    // --- New Font Scaler ---

    /**
     * Calculates a pixel font size based on virtual height.
     * @param heightPercent (0.0 to 1.0) of virtual height.
     * @return The calculated integer pixel size for the font generator.
     */
    public int getFontSize(float heightPercent) {
        return (int) (VIRTUAL_HEIGHT * heightPercent);
    }
}
