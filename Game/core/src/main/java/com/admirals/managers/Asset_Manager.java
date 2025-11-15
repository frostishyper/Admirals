package com.admirals.managers;

import com.badlogic.gdx.Gdx;
import com.badlogic.gdx.assets.AssetManager;
import com.badlogic.gdx.assets.loaders.FileHandleResolver;
import com.badlogic.gdx.assets.loaders.resolvers.InternalFileHandleResolver;
import com.badlogic.gdx.audio.Music;
import com.badlogic.gdx.audio.Sound;
import com.badlogic.gdx.graphics.Color;
import com.badlogic.gdx.graphics.Pixmap;
import com.badlogic.gdx.graphics.Texture;
import com.badlogic.gdx.graphics.g2d.BitmapFont;
import com.badlogic.gdx.graphics.g2d.TextureAtlas;
import com.badlogic.gdx.graphics.g2d.TextureRegion;
import com.badlogic.gdx.graphics.g2d.freetype.FreeTypeFontGenerator;
import com.badlogic.gdx.graphics.g2d.freetype.FreeTypeFontGeneratorLoader;

import java.util.HashMap;

/**
 * Manages loading/access for all assets.
 * Does not handle font generation, only loads font files.
 * Supports multiple atlases, searchable by region name.
 */
public class Asset_Manager {

    public static final AssetManager manager = new AssetManager();
    private static final String TAG = "Asset_Manager";

    // --- Asset Paths ---
    // All atlases to load.
    private static final String[] ATLAS_PATHS = {

        // Add "textures/ui.atlas" here later, etc.
    };

    // --- Logical Key Maps ---
    // For assets accessed by a key, not a region name.
    private static final HashMap<String, String> textureKeys = new HashMap<>();
    private static final HashMap<String, String> soundKeys = new HashMap<>();
    private static final HashMap<String, String> musicKeys = new HashMap<>();
    private static final HashMap<String, String> fontKeys = new HashMap<>(); // .fnt files
    private static final HashMap<String, String> generatorKeys = new HashMap<>(); // .ttf files

    /**
     * Initializes key maps with logical names and file paths.
     */
    private static void initKeyMaps() {
        // Standalone Textures

        // Sounds

        // Music

        // Fonts (File access, not generation)
        generatorKeys.put("8bit_ttf", "fonts/8_bit_operator.ttf");
    }

    /**
     * Queues all assets for loading.
     */
    public static void load() {
        // Set loader for .ttf files
        FileHandleResolver resolver = new InternalFileHandleResolver();
        manager.setLoader(FreeTypeFontGenerator.class, new FreeTypeFontGeneratorLoader(resolver));

        initKeyMaps();

        // Queue all assets
        // Atlases
        for (String path : ATLAS_PATHS) manager.load(path, TextureAtlas.class);
        // Standalone Textures
        for (String path : textureKeys.values()) manager.load(path, Texture.class);
        // Audio
        for (String path : soundKeys.values()) manager.load(path, Sound.class);
        for (String path : musicKeys.values()) manager.load(path, Music.class);
        // Fonts
        for (String path : fontKeys.values()) manager.load(path, BitmapFont.class);
        for (String path : generatorKeys.values()) manager.load(path, FreeTypeFontGenerator.class);

        // Block until loading is complete
        manager.finishLoading();
    }

    // --- Public Getters ---

    /**
     * Gets a region from any loaded atlas.
     * @param name The name of the region (e.g., "ship_red").
     * @return The TextureRegion, or null if not found.
     */
    public static TextureRegion getRegion(String name) {
        TextureRegion region;
        for (String path : ATLAS_PATHS) {
            if (!manager.isLoaded(path)) continue; // Not loaded
            TextureAtlas atlas = manager.get(path, TextureAtlas.class);
            region = atlas.findRegion(name);
            if (region != null) {
                return region; // Found it
            }
        }
        // Not found in any atlas
        Gdx.app.error(TAG, "Region not found in any atlas: " + name);
        return null;
    }

    /**
     * Gets a standalone texture by its logical key.
     * @param key The key (e.g., "background").
     * @return The Texture, or null if not found.
     */
    public static Texture getTexture(String key) {
        String path = textureKeys.get(key);
        if (path == null || !manager.isLoaded(path, Texture.class)) {
            Gdx.app.error(TAG, "Texture not found for key: " + key);
            return null;
        }
        return manager.get(path, Texture.class);
    }

    /**
     * Gets a sound by its logical key.
     */
    public static Sound getSound(String key) {
        String path = soundKeys.get(key);
        if (path == null || !manager.isLoaded(path, Sound.class)) {
            Gdx.app.error(TAG, "Sound not found for key: " + key);
            return null; // No fallback for sound
        }
        return manager.get(path, Sound.class);
    }

    /**
     * Gets music by its logical key.
     */
    public static Music getMusic(String key) {
        String path = musicKeys.get(key);
        if (path == null || !manager.isLoaded(path, Music.class)) {
            Gdx.app.error(TAG, "Music not found for key: " + key);
            return null; // No fallback for music
        }
        return manager.get(path, Music.class);
    }

    /**
     * Gets a preloaded BitmapFont (.fnt) by key.
     */
    public static BitmapFont getFont(String key) {
        String path = fontKeys.get(key);
        if (path == null || !manager.isLoaded(path, BitmapFont.class)) {
            Gdx.app.error(TAG, "BitmapFont not found for key: " + key);
            return null; // Must be handled by caller
        }
        return manager.get(path, BitmapFont.class);
    }

    /**
     * Gets a preloaded font generator (.ttf) by key.
     * A FontManager will use this to create fonts.
     */
    public static FreeTypeFontGenerator getFontGenerator(String key) {
        String path = generatorKeys.get(key);
        if (path == null || !manager.isLoaded(path, FreeTypeFontGenerator.class)) {
            Gdx.app.error(TAG, "FreeTypeFontGenerator not found for key: " + key);
            return null; // Must be handled by caller
        }
        return manager.get(path, FreeTypeFontGenerator.class);
    }

    /**
     * Disposes all loaded assets.
     */
    public static void dispose() {
        manager.dispose();
    }
}
