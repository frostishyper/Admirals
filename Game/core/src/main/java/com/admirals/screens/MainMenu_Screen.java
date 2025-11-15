package com.admirals.screens;

import com.admirals.managers.Font_Manager;
import com.admirals.utils.Scaling_Util;
import com.badlogic.gdx.Game;
import com.badlogic.gdx.Gdx;
import com.badlogic.gdx.Screen;
import com.badlogic.gdx.graphics.Color;
import com.badlogic.gdx.graphics.GL20;
import com.badlogic.gdx.graphics.g2d.SpriteBatch;
import com.badlogic.gdx.utils.Align;

/** First screen of the application. Displays main menu. */
public class MainMenu_Screen implements Screen {

    private final Game game;
    private final Scaling_Util scaler;
    private SpriteBatch batch;

    /**
     * Constructor for the main menu.
     * @param game The main game instance (for switching screens).
     * @param scaler The scaling utility.
     */
    public MainMenu_Screen(Game game, Scaling_Util scaler) {
        this.game = game;
        this.scaler = scaler;
    }

    @Override
    public void show() {
        batch = new SpriteBatch();
        // Set BG Color Temporarily
        Gdx.gl.glClearColor(0.1f, 0.1f, 0.1f, 1);
    }

    @Override
    public void render(float delta) {
        // Clear screen
        Gdx.gl.glClear(GL20.GL_COLOR_BUFFER_BIT);

        // Apply scaling
        scaler.apply();
        batch.setProjectionMatrix(scaler.getCamera().combined);

        // Draw
        batch.begin();
        // Draw "Hello World Text"
        Font_Manager.draw(batch, "Hello World", 0.5f, 0.75f, 0.08f, Color.WHITE, Align.center);
        batch.end();
    }

    @Override
    public void resize(int width, int height) {
        if(width <= 0 || height <= 0) return;
        // Update the viewport
        scaler.update(width, height);
    }

    @Override
    public void pause() { }

    @Override
    public void resume() { }

    @Override
    public void hide() { }

    @Override
    public void dispose() {
        batch.dispose();
    }
}
