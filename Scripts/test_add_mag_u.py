import math
import os
import sys
import tempfile
import unittest

sys.path.insert(0, os.path.dirname(__file__))
from add_mag_u import parse_u_file

VGREAT = -1.7976931e+307  # OpenFOAM sentinel for probes inside solid walls


def _write_u_file(path, time, vectors):
    """Write a minimal OpenFOAM probes U file."""
    lines = [f'# Probe {i} (0 0 0)' for i in range(len(vectors))]
    vec_str = ' '.join(f'({u} {v} {w})' for u, v, w in vectors)
    lines.append(f'{time} {vec_str}')
    with open(path, 'w') as f:
        f.write('\n'.join(lines))


class TestParseUFile(unittest.TestCase):

    def setUp(self):
        tmp = tempfile.NamedTemporaryFile(mode='w', suffix='.txt', delete=False)
        tmp.close()
        self.path = tmp.name

    def tearDown(self):
        os.unlink(self.path)

    def _write(self, time, vectors):
        _write_u_file(self.path, time, vectors)

    # ------------------------------------------------------------------
    # Regression: time token was parsed as the first velocity component,
    # causing overflow to inf and misaligned probe indices.
    # ------------------------------------------------------------------

    def test_time_token_skipped_single_probe(self):
        """Leading time value must not be consumed as a velocity component."""
        # (3, 4, 0) → mag = sqrt(9+16)/5 = 1.0
        self._write(600, [(3.0, 4.0, 0.0)])
        result = parse_u_file(self.path)
        self.assertAlmostEqual(result[0], 1.0, places=10)

    def test_time_token_skipped_multiple_probes(self):
        """All probes must stay aligned after the time token is skipped."""
        self._write(600, [(3.0, 4.0, 0.0), (0.0, 5.0, 0.0)])
        result = parse_u_file(self.path)
        self.assertEqual(len(result), 2)
        self.assertAlmostEqual(result[0], 1.0, places=10)
        self.assertAlmostEqual(result[1], 1.0, places=10)

    # ------------------------------------------------------------------
    # Regression: only the u component was checked for VGREAT; v or w
    # with VGREAT slipped through and produced inf in the output.
    # ------------------------------------------------------------------

    def test_vgreat_in_u_produces_nan(self):
        self._write(600, [(VGREAT, 1.0, 1.0)])
        self.assertTrue(math.isnan(parse_u_file(self.path)[0]))

    def test_vgreat_in_v_produces_nan(self):
        self._write(600, [(1.0, VGREAT, 1.0)])
        self.assertTrue(math.isnan(parse_u_file(self.path)[0]))

    def test_vgreat_in_w_produces_nan(self):
        self._write(600, [(1.0, 1.0, VGREAT)])
        self.assertTrue(math.isnan(parse_u_file(self.path)[0]))

    def test_all_vgreat_produces_nan(self):
        self._write(600, [(VGREAT, VGREAT, VGREAT)])
        self.assertTrue(math.isnan(parse_u_file(self.path)[0]))

    # ------------------------------------------------------------------
    # No inf values may appear in the output under any input combination.
    # ------------------------------------------------------------------

    def test_no_inf_in_output(self):
        probes = [(3.0, 4.0, 0.0), (VGREAT, VGREAT, VGREAT), (1.0, VGREAT, 0.0)]
        self._write(600, probes)
        result = parse_u_file(self.path)
        for val in result:
            self.assertFalse(math.isinf(val), f"Unexpected inf in output: {result}")

    # ------------------------------------------------------------------
    # Multiple time steps: only the last line must be used.
    # ------------------------------------------------------------------

    def test_multiple_time_steps_uses_last(self):
        with open(self.path, 'w') as f:
            f.write('# Probe 0 (0 0 0)\n')
            f.write('100 (0.0 0.0 0.0)\n')  # early step — zero vector
            f.write('600 (3.0 4.0 0.0)\n')  # latest step — non-zero
        result = parse_u_file(self.path)
        self.assertEqual(len(result), 1)
        self.assertAlmostEqual(result[0], 1.0, places=10)

    # ------------------------------------------------------------------
    # Correct magnitude formula: sqrt(u² + v² + w²) / 5
    # ------------------------------------------------------------------

    def test_magnitude_formula_u_axis(self):
        self._write(1, [(5.0, 0.0, 0.0)])
        self.assertAlmostEqual(parse_u_file(self.path)[0], 1.0, places=10)

    def test_magnitude_formula_w_axis(self):
        self._write(1, [(0.0, 0.0, 5.0)])
        self.assertAlmostEqual(parse_u_file(self.path)[0], 1.0, places=10)

    def test_zero_vector(self):
        self._write(600, [(0.0, 0.0, 0.0)])
        self.assertAlmostEqual(parse_u_file(self.path)[0], 0.0, places=10)

    # ------------------------------------------------------------------
    # Edge cases
    # ------------------------------------------------------------------

    def test_missing_file_returns_none(self):
        self.assertIsNone(parse_u_file('/nonexistent/path/U'))

    def test_only_comments_returns_none(self):
        with open(self.path, 'w') as f:
            f.write('# Probe 0 (0 0 0)\n')
        self.assertIsNone(parse_u_file(self.path))


if __name__ == '__main__':
    unittest.main()
