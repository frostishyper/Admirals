package com.admirals.managers;

import com.badlogic.gdx.Gdx;
import com.badlogic.gdx.assets.AssetManager;
import com.badlogic.gdx.assets.loaders.FileHandleResolver;
import com.badlogic.gdx.assets.loaders.resolvers.InternalFileHandleResolver;
import com.badlogic.gdx.audio.Music;
import com.badlogic.gdx.audio.Sound;
import com.badlogic.gdx.graphics.Texture;
import com.badlogic.gdx.graphics.g2d.TextureAtlas;
import com.badlogic.gdx.graphics.g2d.TextureAtlas.AtlasRegion;
import com.badlogic.gdx.graphics.g2d.freetype.FreeTypeFontGenerator;
import com.badlogic.gdx.graphics.g2d.freetype.FreeTypeFontGeneratorLoader;
import com.badlogic.gdx.utils.Array;
import com.badlogic.gdx.utils.GdxRuntimeException;

import java.util.HashMap;

/**
 * Manages loading, retrieval, and disposal of game assets.
 * Maps logical keys to asset paths. Handles caching and disposal.
 */
public class Asset_Manager {

    private final AssetManager assetManager;
    private static final String TAG = "Asset_Manager";

    // Maps for logical keys to file paths
    private final HashMap<String, String> atlasPaths;
    private final HashMap<String, String> spritePaths;
    private final HashMap<String, String> soundPaths;
    private final HashMap<String, String> musicPaths;
    private final HashMap<String, String> fontPaths;

    /**
     * Initializes the AssetManager, sets up loaders, and populates key maps.
     */
    public Asset_Manager() {
        assetManager = new AssetManager();
        FileHandleResolver resolver = new InternalFileHandleResolver();
        assetManager.setLoader(FreeTypeFontGenerator.class, new FreeTypeFontGeneratorLoader(resolver));

        atlasPaths = new HashMap<String, String>();
        spritePaths = new HashMap<String, String>();
        soundPaths = new HashMap<String, String>();
        musicPaths = new HashMap<String, String>();
        fontPaths = new HashMap<String, String>();

        populateAssetMaps();
    }

    /**
     * Defines all logical key-to-path mappings for game assets.
     */
    private void populateAssetMaps() {
        /*
         * Example asset key mappings.
         * Other code will use the key (e.g., "game_ui"), not the path.
         */

        // atlasPaths.put("game_ui", "textures/spritemaps/game_ui.atlas");
        // atlasPaths.put("ships", "textures/spritemaps/ships.atlas");

        // spritePaths.put("background", "textures/sprites/main_background.png");
        // spritePaths.put("logo", "textures/sprites/game_logo.png");

        // soundPaths.put("laser", "sounds/sfx/laser.ogg");
        // soundPaths.put("explosion", "sounds/sfx/explosion.wav");

        // musicPaths.put("main_theme", "sounds/music/main_theme.mp3");

        // Fonts
        fontPaths.put("Operator", "fonts/8_Bit_Operator.ttf");
        // fontPaths.put("roboto_reg", "fonts/roboto_regular.ttf");
    }

    /**
     * Loads all required game assets synchronously based on the populated maps.
     * Call this during the game's initialization phase.
     */
    public void loadAssets() {
        Gdx.app.log(TAG, "Loading assets...");

        // Queue all assets from the path maps
        for (String path : atlasPaths.values()) {
            assetManager.load(path, TextureAtlas.class);
        }
        for (String path : spritePaths.values()) {
            assetManager.load(path, Texture.class);
        }
        for (String path : soundPaths.values()) {
            assetManager.load(path, Sound.class);
        }
        for (String path : musicPaths.values()) {
            assetManager.load(path, Music.class);
        }
        for (String path : fontPaths.values()) {
            assetManager.load(path, FreeTypeFontGenerator.class);
        }

        try {
            // Block until all assets are loaded
            assetManager.finishLoading();
            Gdx.app.log(TAG, "Assets loaded successfully.");
        } catch (GdxRuntimeException e) {
            Gdx.app.error(TAG, "Error loading assets.", e);
        }
    }

    /**
     * Internal helper to retrieve a loaded asset from the manager.
     * @param path Full path to the asset.
     * @param type The class type of the asset.
     * @return The loaded asset, or null if not found.
     */
    private <T> T getAsset(String path, Class<T> type) {
        if (!assetManager.isLoaded(path, type)) {
            Gdx.app.error(TAG, "Asset not loaded: " + path);
            return null;
        }
        try {
            return assetManager.get(path, type);
        } catch (GdxRuntimeException e) {
            Gdx.app.error(TAG, "Error retrieving asset: " + path, e);
            return null;
        }
    }

    /**
     * Retrieves a standalone Texture sprite by its logical key.
     * @param key The logical key for the sprite (e.g., "logo").
     * @return The Texture, or null if key not found or asset not loaded.
     */
    public Texture getSprite(String key) {
        String path = spritePaths.get(key);
        if (path == null) {
            Gdx.app.error(TAG, "No path found for sprite key: " + key);
            return null;
        }
        return getAsset(path, Texture.class);
    }

    /**
     * Retrieves a Sound effect by its logical key.
     * @param key The logical key for the sound (e.g., "laser").
     * @return The Sound, or null if key not found or asset not loaded.
     */
    public Sound getSound(String key) {
        String path = soundPaths.get(key);
        if (path == null) {
            Gdx.app.error(TAG, "No path found for sound key: " + key);
            return null;
        }
        return getAsset(path, Sound.class);
    }

    /**
     * Retrieves a Music stream by its logical key.
     * @param key The logical key for the music (e.g., "main_theme").
     * @return The Music, or null if key not found or asset not loaded.
     */
    public Music getMusic(String key) {
        String path = musicPaths.get(key);
        if (path == null) {
            Gdx.app.error(TAG, "No path found for music key: " + key);
            return null;
        }
        return getAsset(path, Music.class);
    }

    /**
     * Retrieves a FreeTypeFontGenerator by its logical key.
     * @param key The logical key for the font (e.g., "orbitron_bold").
     * @return The FreeTypeFontGenerator, or null if key not found.
     */
    public FreeTypeFontGenerator getFontGenerator(String key) {
        String path = fontPaths.get(key);
        if (path == null) {
            Gdx.app.error(TAG, "No path found for font key: " + key); // Typo 'D +' is fixed here
            return null;
        }
        return getAsset(path, FreeTypeFontGenerator.class);
    }

    /**
     * Retrieves a specific sprite (AtlasRegion) from a TextureAtlas.
     * @param atlasKey The logical key for the atlas (e.g., "game_ui").
     * @param spriteName The name of the sprite within the atlas.
     * @return The AtlasRegion, or null if atlas or sprite not found.
     */
    public AtlasRegion getAtlasSprite(String atlasKey, String spriteName) {
        String atlasPath = atlasPaths.get(atlasKey);
        if (atlasPath == null) {
            Gdx.app.error(TAG, "No path found for atlas key: " + atlasKey);
            return null;
        }

        TextureAtlas atlas = getAsset(atlasPath, TextureAtlas.class);
        if (atlas == null) {
            return null; // getAsset already logged the error
        }

        AtlasRegion region = atlas.findRegion(spriteName);
        if (region == null) {
            Gdx.app.error(TAG, "Sprite '" + spriteName + "' not found in atlas: " + atlasKey); // Typo 'GDEx' fixed
        }
        return region;
    }

    /**
     * Retrieves a specific sprite (AtlasRegion) from a TextureAtlas by index.
     * @param atlasKey The logical key for the atlas (e.g., "ships").
     * @param index The index of the sprite within the atlas.
     * @return The AtlasRegion, or null if atlas not found or index is out of bounds.
     */
    public AtlasRegion getAtlasSprite(String atlasKey, int index) {
        String atlasPath = atlasPaths.get(atlasKey);
        if (atlasPath == null) {
            Gdx.app.error(TAG, "No path found for atlas key: " + atlasKey);
            return null;
        }

        TextureAtlas atlas = getAsset(atlasPath, TextureAtlas.class);
        if (atlas == null) {
            return null; // getAsset already logged the error
        }

        Array<AtlasRegion> regions = atlas.getRegions();
        if (index < 0 || index >= regions.size) {
            Gdx.app.error(TAG, "Index " + index + " out of bounds for atlas: " + atlasKey);
            return null;
        }
        return regions.get(index);
    }

    /**
     * Disposes all assets loaded by this manager.
     * Call this when the game is closing.
     */
    public void dispose() {
        Gdx.app.log(TAG, "Disposing all assets.");
        assetManager.dispose();
    }
}
