package com.admirals.managers;

import com.badlogic.gdx.Gdx;
import com.badlogic.gdx.graphics.OrthographicCamera;
import com.badlogic.gdx.math.Vector2;
import com.badlogic.gdx.utils.viewport.FitViewport;
import com.badlogic.gdx.utils.viewport.Viewport;

import java.util.ArrayList;
import java.util.Collections;
import java.util.HashMap;
import java.util.List;
import java.util.Map;

/**
 * Manages screen scaling, camera, and viewport.
 * Maintains a 1920x1080 virtual resolution using FitViewport.
 * Organizes scalable elements into layers (1-10) for rendering.
 */
public class ScreenScaler_Manager {

    /**
     * Elements managed by the scaler must implement this interface.
     * This allows the manager to sort elements by their rendering layer.
     */
    public interface ScalableElement {
        /**
         * @return The rendering layer ID (1-10). Lower numbers are drawn first (behind).
         */
        int getLayer();
        // Note: The element itself is responsible for holding its virtual
        // position (posX, posY) and size (width, height).
        // The viewport handles the scaling during rendering.
    }

    public static final float VIRTUAL_WIDTH = 1920f;
    public static final float VIRTUAL_HEIGHT = 1080f;

    public static final int MIN_LAYER = 1;
    public static final int MAX_LAYER = 10;

    private static final String TAG = "ScreenScaler_Manager";

    private final OrthographicCamera camera;
    private final FitViewport viewport;

    // Stores elements organized by their layer ID
    private final Map<Integer, List<ScalableElement>> layerMap;
    // Cached list of sorted layer keys for ordered rendering
    private final List<Integer> sortedLayerKeys;

    /**
     * Initializes the camera and viewport for a 1920x1080 virtual resolution.
     */
    public ScreenScaler_Manager() {
        camera = new OrthographicCamera();
        viewport = new FitViewport(VIRTUAL_WIDTH, VIRTUAL_HEIGHT, camera);
        layerMap = new HashMap<Integer, List<ScalableElement>>();
        sortedLayerKeys = new ArrayList<Integer>();
    }

    /**
     * Adds a scalable element to the manager.
     * The element is placed in its correct layer automatically.
     * Logs an error and rejects elements outside the 1-10 layer range.
     * @param element The element to add (e.g., an Element_Ui instance).
     */
    public void addElement(ScalableElement element) {
        int layer = element.getLayer();

        // Validate layer range
        if (layer < MIN_LAYER || layer > MAX_LAYER) {
            Gdx.app.error(TAG, "Element added with invalid layer: " + layer +
                ". Layer must be between " + MIN_LAYER + " and " + MAX_LAYER + ".");
            return; // Do not add the element
        }

        // Find or create the list for this layer
        List<ScalableElement> list = layerMap.get(layer);
        if (list == null) {
            list = new ArrayList<ScalableElement>();
            layerMap.put(layer, list);

            // Add new layer key and re-sort the key list
            // This ensures layers are always in correct render order (1, 2, 3...)
            if (!sortedLayerKeys.contains(layer)) {
                sortedLayerKeys.add(layer);
                Collections.sort(sortedLayerKeys);
            }
        }
        list.add(element);
    }

    /**
     * Removes a scalable element from the manager.
     * If the layer becomes empty, it is removed from internal tracking.
     * @param element The element to remove.
     */
    public void removeElement(ScalableElement element) {
        int layer = element.getLayer();

        // Check if layer is valid before proceeding
        if (layer < MIN_LAYER || layer > MAX_LAYER) {
            Gdx.app.error(TAG, "Attempted to remove element with invalid layer: " + layer);
            return;
        }

        List<ScalableElement> list = layerMap.get(layer);

        if (list != null) {
            list.remove(element);
            // If the layer is now empty, remove it from the maps
            if (list.isEmpty()) {
                layerMap.remove(layer);
                sortedLayerKeys.remove(Integer.valueOf(layer)); // Remove by object, not index
            }
        }
    }

    /**
     * Updates the viewport and camera on window resize.
     * This method must be called from the main game's resize() method.
     * @param screenWidth The new screen width.
     * @param screenHeight The new screen height.
     */
    public void resize(int screenWidth, int screenHeight) {
        // Update the viewport, which applies scaling and letterboxing
        viewport.update(screenWidth, screenHeight, true);
    }

    /**
     * This method is not strictly required per frame if elements do not move.
     * The viewport's resize() call handles all scaling.
     * It is kept for conceptual alignment with the request.
     */
    public void update() {
        // The FitViewport automatically handles scaling.
        // Element positions are virtual (1920x1080).
        // This method is only needed if manual, per-frame updates were required.
    }

    /**
     * Provides the scale factor calculated by the viewport.
     * Useful for scaling non-world items, like fonts.
     * @return The global scale factor.
     */
    public float getScale() {
        // Calculate scale: (Pixel size of viewport) / (Virtual size of viewport)
        // For FitViewport, both X and Y scales are identical.
        return viewport.getScreenWidth() / VIRTUAL_WIDTH;
    }

    /**
     * Converts virtual coordinates (e.g., 100, 100) to actual screen
     * coordinates (e.g., 50, 50).
     * Useful for positioning non-LibGDX elements or debugging.
     * @param virtualX The X position in the 1920x1080 virtual space.
     * @param virtualY The Y position in the 1920x1080 virtual space.
     * @return A Vector2 containing the actual screen pixel coordinates.
     */
    public Vector2 getScaledPosition(float virtualX, float virtualY) {
        Vector2 screenCoords = new Vector2(virtualX, virtualY);
        // Project converts virtual world coordinates to screen coordinates
        viewport.project(screenCoords);
        return screenCoords;
    }

    /**
     * @return The main game camera, managed by this scaler.
     */
    public OrthographicCamera getCamera() {
        return camera;
    }

    /**
     * @return The FitViewport, managed by this scaler.
     */
    public Viewport getViewport() {
        return viewport;
    }

    /**
     * @return The map of all elements, keyed by their layer ID.
     */
    public Map<Integer, List<ScalableElement>> getLayerMap() {
        return layerMap;
    }

    /**
     * @return A sorted list of layer IDs (1-10). Iterate this list to draw layers in order.
     */
    public List<Integer> getSortedLayerKeys() {
        return sortedLayerKeys;
    }
}
