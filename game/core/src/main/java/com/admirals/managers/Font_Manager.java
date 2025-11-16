package com.admirals.managers;

import com.badlogic.gdx.Gdx;
import com.badlogic.gdx.graphics.Color;
import com.badlogic.gdx.graphics.Texture;
import com.badlogic.gdx.graphics.g2d.BitmapFont;
import com.badlogic.gdx.graphics.g2d.SpriteBatch;
import com.badlogic.gdx.graphics.g2d.freetype.FreeTypeFontGenerator;
// import com.badlogic.gdx.math.Vector2; // No longer needed
import com.badlogic.gdx.utils.Disposable;

import java.util.HashMap;
import java.util.Map;

/**
 * Manages font generation, caching, and drawing.
 * Integrates with Asset_Manager and ScreenScaler_Manager.
 * All sizes and positions are relative to the virtual 1920x1080 resolution.
 */
public class Font_Manager implements Disposable {

    private static final String TAG = "Font_Manager";
    private static final String FONT_KEY = "Operator"; // Logical key from Asset_Manager

    private final ScreenScaler_Manager scaler;
    private final FreeTypeFontGenerator operatorGenerator;

    // Caches generated BitmapFonts by their virtual (1920x1080) size
    private final Map<Integer, BitmapFont> fontCache;

    /**
     * Initializes the Font Manager.
     * @param assetManager The game's Asset_Manager.
     * @param scaler The game's ScreenScaler_Manager.
     */
    public Font_Manager(Asset_Manager assetManager, ScreenScaler_Manager scaler) {
        this.scaler = scaler;
        this.operatorGenerator = assetManager.getFontGenerator(FONT_KEY);
        this.fontCache = new HashMap<Integer, BitmapFont>();

        if (this.operatorGenerator == null) {
            Gdx.app.error(TAG, "Failed to load 'Operator' font generator from Asset_Manager.");
        }
    }

    /**
     * Retrieves a cached BitmapFont for a specific *virtual* size.
     * If not cached, generates and caches it.
     * @param virtualSize The target font size in virtual (1920x1080) units.
     * @return The BitmapFont, or null if generation fails.
     */
    private BitmapFont getFont(int virtualSize) {
        if (virtualSize <= 0) {
            Gdx.app.error(TAG, "Invalid font size requested: " + virtualSize);
            return null;
        }

        if (operatorGenerator == null) {
            return null; // Generator failed to load in constructor
        }

        // Check cache
        if (fontCache.containsKey(virtualSize)) {
            return fontCache.get(virtualSize);
        }

        // Not cached, generate new font
        Gdx.app.log(TAG, "Generating 'Operator' font at size " + virtualSize + " units");

        FreeTypeFontGenerator.FreeTypeFontParameter parameter = new FreeTypeFontGenerator.FreeTypeFontParameter();
        parameter.size = virtualSize;
        parameter.minFilter = Texture.TextureFilter.Linear; // For smooth scaling
        parameter.magFilter = Texture.TextureFilter.Linear;

        try {
            BitmapFont newFont = operatorGenerator.generateFont(parameter);
            fontCache.put(virtualSize, newFont);
            return newFont;
        } catch (Exception e) {
            Gdx.app.error(TAG, "Failed to generate font at size " + virtualSize, e);
            return null;
        }
    }

    /**
     * Draws text in virtual-space coordinates.
     * Assumes the SpriteBatch projection matrix is set to scaler.getCamera().combined.
     *
     * @param batch The SpriteBatch (set for virtual-space).
     * @param text The text to draw.
     * @param color The color of the text.
     * @param size The font size in virtual (1920x1080) units.
     * @param posX The X position in virtual (1920x1080) units.
     * @param posY The Y position in virtual (1920x1080) units.
     */
    public void draw(SpriteBatch batch, String text, Color color, float size, float posX, float posY) {
        // 1. Calculate virtual size
        int virtualSize = Math.round(size);

        // 2. Get the font for that virtual size (from cache or generate)
        BitmapFont font = getFont(virtualSize);
        if (font == null) {
            return; // Error logged in getFont
        }

        // 3. Save original color, set new color, draw, and restore.
        // This prevents modifying the cached font instance.
        Color oldColor = font.getColor();
        font.setColor(color);
        font.draw(batch, text, posX, posY);
        font.setColor(oldColor);
    }

    /**
     * Disposes all cached BitmapFont instances.
     * Does not dispose the generator, as Asset_Manager owns it.
     */
    @Override
    public void dispose() {
        Gdx.app.log(TAG, "Disposing all cached fonts.");
        for (BitmapFont font : fontCache.values()) {
            font.dispose();
        }
        fontCache.clear();
    }
}
