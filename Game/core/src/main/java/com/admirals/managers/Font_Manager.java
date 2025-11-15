package com.admirals.managers;

import com.admirals.utils.Scaling_Util;
import com.badlogic.gdx.Gdx;
import com.badlogic.gdx.graphics.Color;
import com.badlogic.gdx.graphics.Texture;
import com.badlogic.gdx.graphics.g2d.BitmapFont;
import com.badlogic.gdx.graphics.g2d.GlyphLayout;
import com.badlogic.gdx.graphics.g2d.SpriteBatch;
import com.badlogic.gdx.graphics.g2d.freetype.FreeTypeFontGenerator;
import com.badlogic.gdx.graphics.g2d.freetype.FreeTypeFontGenerator.FreeTypeFontParameter;
import com.badlogic.gdx.utils.Align;

import java.util.HashMap;

/**
 * Manages dynamic font generation, caching, and drawing.
 * Works with Scaling_Util to simplify text rendering.
 */
public class Font_Manager {

    private static final String TAG = "Font_Manager";
    // The TTF key from Asset_Manager
    private static final String DEFAULT_FONT_KEY = "8bit_ttf";

    // Snaps font sizes to nearest 4px.
    private static final int FONT_SIZE_GRANULARITY = 4;

    private static Scaling_Util scaler;
    private static FreeTypeFontGenerator generator;
    private static HashMap<String, BitmapFont> fontCache;
    private static GlyphLayout layout; // For alignment calculations
    private static Color tempColor = new Color(); // Avoids 'new' in render

    /**
     * Initializes the Font Manager.
     * @param appScaler The application's Scaling_Util instance.
     */
    public static void init(Scaling_Util appScaler) {
        scaler = appScaler;
        fontCache = new HashMap<>();
        layout = new GlyphLayout();
        // Get the generator from the Asset_Manager
        generator = Asset_Manager.getFontGenerator(DEFAULT_FONT_KEY);

        if (generator == null) {
            Gdx.app.error(TAG, "Font generator '" + DEFAULT_FONT_KEY + "' not found in Asset_Manager!");
        }
    }

    /**
     * Draws text with specified alignment.
     * This is the primary method for drawing text.
     *
     * @param batch       The SpriteBatch.
     * @param text        The text to draw.
     * @param xPercent    X position (0.0 to 1.0).
     * @param yPercent    Y position (0.0 to 1.0).
     * @param sizePercent Font size (0.0 to 1.0 of virtual height).
     * @param color       The font color.
     * @param align       LibGDX alignment (e.g., Align.center).
     */
    public static void draw(SpriteBatch batch, String text, float xPercent, float yPercent, float sizePercent, Color color, int align) {
        if (generator == null) return; // Not initialized

        // Get the font (from cache or generate).
        BitmapFont font = getFont(sizePercent);
        if (font == null) return; // Generation failed

        // Calculate real coordinates
        float x = scaler.getX(xPercent);
        float y = scaler.getY(yPercent);

        // Set layout to calculate bounds
        layout.setText(font, text);

        // Adjust position based on alignment
        if (Align.isCenterHorizontal(align)) {
            x -= layout.width / 2f;
        } else if (Align.isRight(align)) {
            x -= layout.width;
        }
        // LibGDX draws from top-left, so +Y is up.
        if (Align.isCenterVertical(align)) {
            y += layout.height / 2f;
        } else if (Align.isBottom(align)) {
            y += layout.height;
        }

        // Save batch color, draw, restore.
        tempColor.set(batch.getColor()); // Save
        batch.setColor(color);
        font.draw(batch, layout, x, y);
        batch.setColor(tempColor); // Restore
    }


    /**
     * Draws text with default top-left alignment.
     */
    public static void draw(SpriteBatch batch, String text, float xPercent, float yPercent, float sizePercent, Color color) {
        draw(batch, text, xPercent, yPercent, sizePercent, color, Align.topLeft);
    }

    /**
     * Retrieves a font by size, generating it if not cached.
     * @param sizePercent Font size (0.0 to 1.0 of virtual height).
     * @return A cached BitmapFont.
     */
    public static BitmapFont getFont(float sizePercent) {
        if (generator == null) return null;

        // Convert percent to pixels and normalize
        int pixelSize = (int) scaler.getHeight(sizePercent);
        int normalizedSize = normalize(pixelSize);

        // Check for 0-size
        if (normalizedSize <= 0) {
            Gdx.app.error(TAG, "Attempted to get 0px font.");
            return null;
        }

        String cacheKey = DEFAULT_FONT_KEY + "_" + normalizedSize + "px";

        // Check cache
        if (fontCache.containsKey(cacheKey)) {
            return fontCache.get(cacheKey);
        }

        // Not cached. Generate new font.
        Gdx.app.log(TAG, "Generating font: " + cacheKey);
        FreeTypeFontParameter parameter = new FreeTypeFontParameter();
        parameter.size = normalizedSize;
        parameter.minFilter = Texture.TextureFilter.Linear; // Smooth
        parameter.magFilter = Texture.TextureFilter.Linear; // Smooth

        BitmapFont font = generator.generateFont(parameter);
        fontCache.put(cacheKey, font); // Add to cache
        return font;
    }

    /**
     * Snaps a pixel size to the nearest granularity.
     */
    private static int normalize(int pixelSize) {
        int normalized = (pixelSize / FONT_SIZE_GRANULARITY) * FONT_SIZE_GRANULARITY;
        return Math.max(FONT_SIZE_GRANULARITY, normalized); // Min size
    }

    /**
     * Disposes all cached fonts.
     */
    public static void dispose() {
        Gdx.app.log(TAG, "Disposing " + fontCache.size() + " fonts.");
        for (BitmapFont font : fontCache.values()) {
            font.dispose();
        }
        fontCache.clear();
        // The generator is disposed by Asset_Manager.
    }
}
